using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Data.SqlClient;
using Forms = System.Windows.Forms;

namespace CafeOrders.SetupWizard;

public partial class MainWindow : Window
{
    private const string DefaultPackageUrl = "https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip";

    private readonly string[] _stepTitles =
    [
        "SQL Bağlantısı",
        "IIS ve Paket",
        "Kurulum Seçenekleri",
        "Özet ve Kurulum"
    ];

    private readonly string[] _stepSubtitles =
    [
        "CafeOrders veritabanı için kullanılacak SQL Server bilgilerini girin.",
        "Production paket kaynağını, IIS root dizinini ve servis portlarını belirleyin.",
        "Firewall, WatchDog ve uploads koruma gibi otomasyon adımlarını seçin.",
        "Bilgileri kontrol edin, ön kontrolü çalıştırın ve kurulumu başlatın."
    ];

    private FrameworkElement[] _stepPages = [];
    private TextBlock[] _stepIndicators = [];
    private int _currentStep;
    private bool _isInstalling;

    public MainWindow()
    {
        InitializeComponent();

        _stepPages =
        [
            StepSqlPage,
            StepIisPage,
            StepOptionsPage,
            StepReviewPage
        ];

        _stepIndicators =
        [
            StepSqlIndicator,
            StepIisIndicator,
            StepOptionsIndicator,
            StepReviewIndicator
        ];

        InitializeDefaults();
        AdminWarningBox.Visibility = IsAdministrator() ? Visibility.Collapsed : Visibility.Visible;
        UpdateStepState();
    }

    private void InitializeDefaults()
    {
        PackageUrlBox.Text = DefaultPackageUrl;
        PackagePathBox.Text = string.Empty;
        PopulateServerIpChoices();
        ApiPortBox.Text = "5001";
        WebUiPortBox.Text = "5002";
        PopulateSqlInstanceChoices();
        SqlUserBox.Text = "sa";
        SqlPasswordBox.Password = string.Empty;
        IisRootPathBox.Text = @"C:\inetpub\wwwroot";
        AppendLog("Wizard hazır. Kurulum için yönetici yetkisi önerilir.");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (FindResource("SplashExitStoryboard") is not Storyboard storyboard)
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        storyboard.Completed += (_, _) =>
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            SplashOverlay.IsHitTestVisible = false;
        };
        storyboard.Begin(this, isControllable: false);
    }

    private void PopulateSqlInstanceChoices()
    {
        SqlInstanceBox.Items.Clear();
        var instances = DiscoverSqlInstances();
        foreach (var instance in instances)
        {
            SqlInstanceBox.Items.Add(instance);
        }

        SqlInstanceBox.Text = instances.FirstOrDefault() ?? @".\SQLEXPRESS";
        AppendLog(instances.Count > 0
            ? $"{instances.Count} SQL instance seçeneği bulundu."
            : "SQL instance otomatik bulunamadı. Varsayılan .\\SQLEXPRESS kullanılacak.");
    }

    private void PopulateServerIpChoices()
    {
        ServerIpBox.Items.Clear();
        var addresses = DiscoverServerIps();
        foreach (var address in addresses)
        {
            ServerIpBox.Items.Add(address);
        }

        ServerIpBox.Text = addresses.FirstOrDefault() ?? "192.168.2.11";
        AppendLog(addresses.Count > 0
            ? $"{addresses.Count} aktif IPv4 adresi bulundu."
            : "Aktif IPv4 adresi otomatik bulunamadı. IP alanı manuel düzenlenebilir.");
    }

    private void BrowsePackageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "CafeOrders paketini seç",
            Filter = "Zip package (*.zip)|*.zip|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            PackagePathBox.Text = dialog.FileName;
            AppendLog($"Local paket seçildi: {dialog.FileName}");
        }
    }

    private void BrowseIisRootButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "IIS root klasörünü seçin",
            SelectedPath = Directory.Exists(IisRootPathBox.Text) ? IisRootPathBox.Text : @"C:\inetpub\wwwroot",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            IisRootPathBox.Text = dialog.SelectedPath;
        }
    }

    private async void TestSqlButton_Click(object sender, RoutedEventArgs e)
    {
        TestSqlButton.IsEnabled = false;
        SqlTestStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextMuted");
        SqlTestStatusText.Text = "SQL bağlantısı test ediliyor...";

        try
        {
            _ = Required(SqlInstanceBox.Text, "SQL Instance");
            _ = Required(SqlUserBox.Text, "SQL kullanıcı");
            _ = Required(SqlPasswordBox.Password, "SQL şifre");

            await TestSqlConnectionAsync();
            SqlTestStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(19, 209, 142));
            SqlTestStatusText.Text = "SQL bağlantısı başarılı. Kimlik bilgileri doğrulandı.";
            AppendLog("SQL bağlantı testi başarılı.");
        }
        catch (Exception ex)
        {
            SqlTestStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 122, 143));
            SqlTestStatusText.Text = $"SQL bağlantısı başarısız: {ex.Message}";
            AppendLog($"SQL bağlantı testi başarısız: {ex.Message}");
            System.Windows.MessageBox.Show(this, ex.Message, "SQL Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            TestSqlButton.IsEnabled = true;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling || _currentStep == 0)
        {
            return;
        }

        _currentStep--;
        UpdateStepState();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling || !ValidateCurrentStep())
        {
            return;
        }

        if (_currentStep < _stepPages.Length - 1)
        {
            _currentStep++;
            UpdateStepState();
        }
    }

    private bool ValidateCurrentStep()
    {
        try
        {
            switch (_currentStep)
            {
                case 0:
                    _ = Required(SqlInstanceBox.Text, "SQL Instance");
                    _ = Required(SqlUserBox.Text, "SQL kullanıcı");
                    _ = Required(SqlPasswordBox.Password, "SQL şifre");
                    break;
                case 1:
                    _ = Required(ServerIpBox.Text, "Server IP");
                    _ = ParsePort(ApiPortBox.Text, "API port");
                    _ = ParsePort(WebUiPortBox.Text, "WebUI port");
                    _ = Required(IisRootPathBox.Text, "IIS root path");
                    if (string.IsNullOrWhiteSpace(PackagePathBox.Text))
                    {
                        _ = Required(PackageUrlBox.Text, "Paket kaynak URL");
                    }
                    else if (!File.Exists(PackagePathBox.Text) && !Directory.Exists(PackagePathBox.Text))
                    {
                        throw new InvalidOperationException("Seçilen local paket bulunamadı.");
                    }
                    break;
                case 3:
                    _ = BuildConfig();
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"Adım doğrulaması başarısız: {ex.Message}");
            System.Windows.MessageBox.Show(this, ex.Message, "CafeOrders Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void PrecheckButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = BuildConfig();
            var script = ResolveInstallerScript();
            AppendLog($"Installer script bulundu: {script}");
            AppendLog(IsAdministrator()
                ? "Yönetici yetkisi doğrulandı."
                : "Uyarı: Wizard yönetici olarak çalışmıyor. Kurulum adımı IIS/firewall/task işlemlerinde hata verebilir.");
            AppendLog("Ön kontrol tamamlandı.");
        }
        catch (Exception ex)
        {
            AppendLog($"Ön kontrol başarısız: {ex.Message}");
            System.Windows.MessageBox.Show(this, ex.Message, "Ön Kontrol", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateCurrentStep())
        {
            return;
        }

        SetBusy(true);

        string? configPath = null;
        try
        {
            var config = BuildConfig();
            var script = ResolveInstallerScript();
            configPath = Path.Combine(Path.GetTempPath(), $"CafeOrdersSetup_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

            AppendLog("Kurulum başlatılıyor...");
            var exitCode = await RunInstallerAsync(script, configPath);
            if (exitCode == 0)
            {
                AppendLog("Kurulum başarıyla tamamlandı.");
                DownloadDesktopButton.Visibility = Visibility.Visible;
                DownloadDesktopButton.IsEnabled = true;
                System.Windows.MessageBox.Show(this, "CafeOrders kurulumu tamamlandı.", "CafeOrders Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                AppendLog($"Kurulum hata kodu ile tamamlandı: {exitCode}");
                System.Windows.MessageBox.Show(this, $"Kurulum tamamlanamadı. ExitCode={exitCode}", "CafeOrders Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Kurulum hatası: {ex.Message}");
            System.Windows.MessageBox.Show(this, ex.Message, "CafeOrders Setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                TryDelete(configPath);
            }

            SetBusy(false);
        }
    }

    private async void DownloadDesktopButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateCurrentStep())
        {
            return;
        }

        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "DesktopApp dosyalarının indirileceği klasörü seçin",
            SelectedPath = Directory.Exists(@"C:\DesktopApp") ? @"C:\DesktopApp" : @"C:\",
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        SetBusy(true);
        DownloadDesktopButton.IsEnabled = false;

        try
        {
            var config = BuildConfig();
            AppendLog("DesktopApp paketi hazırlanıyor...");

            using var package = await ResolvePackageRootAsync();
            var source = Path.Combine(package.RootPath, "publishes", "DesktopApp");
            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException("Paket içinde publishes\\DesktopApp klasörü bulunamadı.");
            }

            CopyDirectoryContents(source, dialog.SelectedPath);
            await WriteDesktopAppSettingsAsync(dialog.SelectedPath, config);

            AppendLog($"DesktopApp hazırlandı: {dialog.SelectedPath}");
            System.Windows.MessageBox.Show(this, "DesktopApp dosyaları seçilen klasöre hazırlandı.", "DesktopApp İndir", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"DesktopApp hazırlama hatası: {ex.Message}");
            System.Windows.MessageBox.Show(this, ex.Message, "DesktopApp İndir", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
            DownloadDesktopButton.IsEnabled = true;
        }
    }

    private void UpdateStepState()
    {
        StepTitleText.Text = _stepTitles[_currentStep];
        StepSubtitleText.Text = _stepSubtitles[_currentStep];

        for (var index = 0; index < _stepPages.Length; index++)
        {
            _stepPages[index].Visibility = index == _currentStep ? Visibility.Visible : Visibility.Collapsed;
            _stepIndicators[index].Foreground = index == _currentStep
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("TextMuted");
            _stepIndicators[index].Opacity = index <= _currentStep ? 1 : 0.55;
        }

        BackButton.IsEnabled = !_isInstalling && _currentStep > 0;
        NextButton.Visibility = _currentStep == _stepPages.Length - 1 ? Visibility.Collapsed : Visibility.Visible;
        PrecheckButton.Visibility = _currentStep == _stepPages.Length - 1 ? Visibility.Visible : Visibility.Collapsed;
        InstallButton.Visibility = _currentStep == _stepPages.Length - 1 ? Visibility.Visible : Visibility.Collapsed;

        if (_currentStep == _stepPages.Length - 1)
        {
            RefreshReviewSummary();
        }
    }

    private void RefreshReviewSummary()
    {
        ReviewSummaryText.Text =
            $"SQL: {SqlInstanceBox.Text.Trim()} / {SqlUserBox.Text.Trim()}{Environment.NewLine}" +
            $"API: http://{ServerIpBox.Text.Trim()}:{ApiPortBox.Text.Trim()}{Environment.NewLine}" +
            $"WebUI: http://{ServerIpBox.Text.Trim()}:{WebUiPortBox.Text.Trim()}{Environment.NewLine}" +
            $"IIS Root: {IisRootPathBox.Text.Trim()}{Environment.NewLine}" +
            $"Paket: {(string.IsNullOrWhiteSpace(PackagePathBox.Text) ? PackageUrlBox.Text.Trim() : PackagePathBox.Text.Trim())}{Environment.NewLine}" +
            $"Firewall: {FormatBool(OpenFirewallBox.IsChecked == true)}, WatchDog: {FormatBool(RegisterTaskBox.IsChecked == true)}, İlk tetik: {FormatBool(TriggerTaskBox.IsChecked == true)}, Uploads koruma: {FormatBool(PreserveUploadsBox.IsChecked == true)}";
    }

    private void SetBusy(bool isBusy)
    {
        _isInstalling = isBusy;
        InstallProgress.IsIndeterminate = isBusy;
        BackButton.IsEnabled = !isBusy && _currentStep > 0;
        NextButton.IsEnabled = !isBusy;
        PrecheckButton.IsEnabled = !isBusy;
        InstallButton.IsEnabled = !isBusy;
        DownloadDesktopButton.IsEnabled = !isBusy && DownloadDesktopButton.Visibility == Visibility.Visible;
    }

    private async Task TestSqlConnectionAsync()
    {
        var connectionString = $"Server={Required(SqlInstanceBox.Text, "SQL Instance")};Database=master;User Id={Required(SqlUserBox.Text, "SQL kullanıcı")};Password={Required(SqlPasswordBox.Password, "SQL şifre")};TrustServerCertificate=True;Encrypt=False;Connection Timeout=5";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
    }

    private async Task<PackageRootHandle> ResolvePackageRootAsync()
    {
        var packagePath = string.IsNullOrWhiteSpace(PackagePathBox.Text) ? null : PackagePathBox.Text.Trim();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"CafeOrdersSetupPackage_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        if (!string.IsNullOrWhiteSpace(packagePath))
        {
            if (!File.Exists(packagePath) && !Directory.Exists(packagePath))
            {
                throw new FileNotFoundException("Seçilen paket bulunamadı.", packagePath);
            }

            if (Directory.Exists(packagePath))
            {
                return new PackageRootHandle(packagePath, null);
            }

            ZipFile.ExtractToDirectory(packagePath, tempRoot, overwriteFiles: true);
        }
        else
        {
            var zipPath = Path.Combine(tempRoot, "CafeOrders-Production.zip");
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(string.IsNullOrWhiteSpace(PackageUrlBox.Text) ? DefaultPackageUrl : PackageUrlBox.Text.Trim());
            await using var file = File.Create(zipPath);
            await stream.CopyToAsync(file);
            file.Close();
            ZipFile.ExtractToDirectory(zipPath, Path.Combine(tempRoot, "package"), overwriteFiles: true);
        }

        var root = FindPackageRoot(tempRoot);
        return new PackageRootHandle(root, tempRoot);
    }

    private static string FindPackageRoot(string root)
    {
        if (Directory.Exists(Path.Combine(root, "publishes")) && Directory.Exists(Path.Combine(root, "scripts")))
        {
            return root;
        }

        var candidate = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                Directory.Exists(Path.Combine(path, "publishes")) &&
                Directory.Exists(Path.Combine(path, "scripts")));

        if (candidate is null)
        {
            throw new DirectoryNotFoundException("Paket içinde publishes ve scripts klasörleri bulunamadı.");
        }

        return candidate;
    }

    private static void CopyDirectoryContents(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static async Task WriteDesktopAppSettingsAsync(string destination, SetupConfig config)
    {
        var settings = new
        {
            Endpoints = new
            {
                ApiBaseUrl = $"http://{config.ServerIp}:{config.ApiPort}/",
                HubUrl = $"http://{config.ServerIp}:{config.ApiPort}/hubs/cafe"
            },
            Media = new
            {
                SharedWebRootPath = $@"\\{config.ServerIp}\inetpub\wwwroot\WebUI\wwwroot"
            },
            Session = new
            {
                AutoCloseAfterSeconds = 150
            },
            Startup = new
            {
                RetryCount = 60,
                RetryDelaySeconds = 2
            }
        };

        var path = Path.Combine(destination, "appsettings.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    private SetupConfig BuildConfig()
    {
        var serverIp = Required(ServerIpBox.Text, "Server IP");
        var sqlInstance = Required(SqlInstanceBox.Text, "SQL Instance");
        var sqlUser = Required(SqlUserBox.Text, "SQL kullanıcı");
        var sqlPassword = Required(SqlPasswordBox.Password, "SQL şifre");
        var iisRootPath = Required(IisRootPathBox.Text, "IIS root path");
        var apiPort = ParsePort(ApiPortBox.Text, "API port");
        var webUiPort = ParsePort(WebUiPortBox.Text, "WebUI port");
        var packagePath = string.IsNullOrWhiteSpace(PackagePathBox.Text) ? null : PackagePathBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(packagePath) && !File.Exists(packagePath) && !Directory.Exists(packagePath))
        {
            throw new InvalidOperationException("Seçilen local paket bulunamadı.");
        }

        return new SetupConfig
        {
            PackageUrl = string.IsNullOrWhiteSpace(PackageUrlBox.Text) ? DefaultPackageUrl : PackageUrlBox.Text.Trim(),
            PackagePath = packagePath,
            ServerIp = serverIp,
            ApiPort = apiPort,
            WebUiPort = webUiPort,
            SqlInstanceName = sqlInstance,
            SqlUser = sqlUser,
            SqlPassword = sqlPassword,
            IisRootPath = iisRootPath,
            OpenFirewall = OpenFirewallBox.IsChecked == true,
            RegisterTask = RegisterTaskBox.IsChecked == true,
            TriggerTask = TriggerTaskBox.IsChecked == true,
            PreserveUploads = PreserveUploadsBox.IsChecked == true
        };
    }

    private async Task<int> RunInstallerAsync(string scriptPath, string configPath)
    {
        var powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = powershell,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ConfigPath \"{configPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Dispatcher.Invoke(() => AppendLog(args.Data));
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Dispatcher.Invoke(() => AppendLog(args.Data));
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string ResolveInstallerScript()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var outputScript = Path.Combine(baseDirectory, "installer", "Install-CafeOrders.ps1");
        if (File.Exists(outputScript))
        {
            return outputScript;
        }

        var directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "installer", "Install-CafeOrders.ps1");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Install-CafeOrders.ps1 bulunamadı.");
    }

    private static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} zorunludur.");
        }

        return value.Trim();
    }

    private static int ParsePort(string value, string name)
    {
        if (!int.TryParse(value, out var port) || port < 1 || port > 65535)
        {
            throw new InvalidOperationException($"{name} geçerli bir TCP port olmalı.");
        }

        return port;
    }

    private static string FormatBool(bool value)
        => value ? "Açık" : "Kapalı";

    private static IReadOnlyList<string> DiscoverSqlInstances()
    {
        var machineName = Environment.MachineName;
        var instances = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (key is null)
                {
                    continue;
                }

                foreach (var name in key.GetValueNames())
                {
                    if (name.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                    {
                        instances.Add(machineName);
                        instances.Add(".");
                    }
                    else
                    {
                        instances.Add($@".\{name}");
                        instances.Add($@"{machineName}\{name}");
                    }
                }
            }
            catch
            {
            }
        }

        if (instances.Count == 0)
        {
            instances.Add(@".\SQLEXPRESS");
            instances.Add($@"{machineName}\SQLEXPRESS");
        }

        return instances.ToArray();
    }

    private static IReadOnlyList<string> DiscoverServerIps()
    {
        var addresses = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
            {
                var address = unicastAddress.Address;
                if (address.AddressFamily != AddressFamily.InterNetwork ||
                    IPAddress.IsLoopback(address) ||
                    address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
                {
                    continue;
                }

                addresses.Add(address.ToString());
            }
        }

        return addresses.ToArray();
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }

    private void AppendLog(string message)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private sealed class SetupConfig
    {
        public string PackageUrl { get; set; } = DefaultPackageUrl;
        public string? PackagePath { get; set; }
        public string ServerIp { get; set; } = string.Empty;
        public int ApiPort { get; set; }
        public int WebUiPort { get; set; }
        public string SqlInstanceName { get; set; } = string.Empty;
        public string SqlUser { get; set; } = string.Empty;
        public string SqlPassword { get; set; } = string.Empty;
        public string IisRootPath { get; set; } = string.Empty;
        public bool OpenFirewall { get; set; }
        public bool RegisterTask { get; set; }
        public bool TriggerTask { get; set; }
        public bool PreserveUploads { get; set; }
    }

    private sealed class PackageRootHandle(string rootPath, string? tempPath) : IDisposable
    {
        public string RootPath { get; } = rootPath;

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                return;
            }

            try
            {
                Directory.Delete(tempPath, recursive: true);
            }
            catch
            {
            }
        }
    }
}
