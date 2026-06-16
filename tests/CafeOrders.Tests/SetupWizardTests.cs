namespace CafeOrders.Tests;

public sealed class SetupWizardTests
{
    [Fact]
    public void SetupWizardProject_IsIncludedAndCarriesInstallerScript()
    {
        var solution = ReadRepoFile("CafeOrders.slnx");
        var project = ReadRepoFile("src", "CafeOrders.SetupWizard", "CafeOrders.SetupWizard.csproj");
        var xaml = ReadRepoFile("src", "CafeOrders.SetupWizard", "MainWindow.xaml");
        var codeBehind = ReadRepoFile("src", "CafeOrders.SetupWizard", "MainWindow.xaml.cs");

        Assert.Contains("src/CafeOrders.SetupWizard/CafeOrders.SetupWizard.csproj", solution);
        Assert.Contains("<UseWPF>true</UseWPF>", project);
        Assert.Contains("<UseWindowsForms>true</UseWindowsForms>", project);
        Assert.Contains("Microsoft.Data.SqlClient", project);
        Assert.Contains("Install-CafeOrders.ps1", project);
        Assert.Contains("CafeOrders Setup Wizard", xaml);
        Assert.Contains("Loaded=\"Window_Loaded\"", xaml);
        Assert.Contains("SplashOverlay", xaml);
        Assert.Contains("SplashExitStoryboard", xaml);
        Assert.Contains("ModeSelectionOverlay", xaml);
        Assert.Contains("Server Kur", xaml);
        Assert.Contains("DesktopApp Kur", xaml);
        Assert.Contains("Kaldır", xaml);
        Assert.Contains("DangerButton", xaml);
        Assert.Contains("AppDialogOverlay", xaml);
        Assert.Contains("OptionCheckBox", xaml);
        Assert.Contains("ComboInput", xaml);
        Assert.Contains("PackageSourceRadio", xaml);
        Assert.Contains("StepSqlPage", xaml);
        Assert.Contains("StepIisPage", xaml);
        Assert.Contains("StepOptionsPage", xaml);
        Assert.Contains("StepReviewPage", xaml);
        Assert.Contains("RemotePackageRadio", xaml);
        Assert.Contains("LocalPackageRadio", xaml);
        Assert.Contains("RemotePackagePanel", xaml);
        Assert.Contains("LocalPackagePanel", xaml);
        Assert.Contains("TestSqlButton", xaml);
        Assert.Contains("SqlTestStatusText", xaml);
        Assert.Contains("DownloadDesktopButton", xaml);
        Assert.Contains("IisRootLabel", xaml);
        Assert.Contains("IisRootPickerGrid", xaml);
        Assert.Contains("DownloadProgressText", xaml);
        Assert.Contains("LogScrollViewer", xaml);
        Assert.Contains("LogTextBlock", xaml);
        Assert.Contains("CopyLogButton_Click", xaml);
        Assert.Contains("⧉  Kopyala", xaml);
        Assert.Contains("InstallHostingBundleBox", xaml);
        Assert.Contains("Height=\"330\"", xaml);
        Assert.Contains("Padding=\"28,22\"", xaml);
        Assert.Contains("Alperen TEKE", xaml);
        Assert.Contains("0 (541) 688 88 06", xaml);
        Assert.Contains("LeftPanelTitleText", xaml);
        Assert.Contains("LeftPanelInfoTitleText", xaml);
        Assert.Contains("ValidationButton", xaml);
        Assert.Contains("https://github.com/alperenteke06/CafeOrders/tree/Production", codeBehind);
        Assert.Contains("ConfigPath", codeBehind);
        Assert.Contains("SqlPasswordBox.Password", codeBehind);
        Assert.Contains("TestSqlButton_Click", codeBehind);
        Assert.Contains("TestSqlConnectionAsync", codeBehind);
        Assert.Contains("ValidateSqlConnectionAsync", codeBehind);
        Assert.Contains("PackageSourceRadio_Checked", codeBehind);
        Assert.Contains("UpdatePackageSourceVisibility", codeBehind);
        Assert.Contains("DownloadDesktopButton_Click", codeBehind);
        Assert.Contains("DownloadDesktopAppToSelectedFolderAsync", codeBehind);
        Assert.Contains("ResolvePackageRootAsync", codeBehind);
        Assert.Contains("DownloadGitHubPackageAsync", codeBehind);
        Assert.Contains("ServerPackagePrefixes", codeBehind);
        Assert.Contains("DesktopPackagePrefixes", codeBehind);
        Assert.Contains("PackageDownloadScope.Server", codeBehind);
        Assert.Contains("PackageDownloadScope.DesktopApp", codeBehind);
        Assert.Contains("FormatPackageScope", codeBehind);
        Assert.Contains("DesktopApp paketi", codeBehind);
        Assert.Contains("server paketi", codeBehind);
        Assert.Contains("raw.githubusercontent.com", codeBehind);
        Assert.Contains("HttpCompletionOption.ResponseHeadersRead", codeBehind);
        Assert.Contains("UpdateDownloadProgress", codeBehind);
        Assert.Contains("PackagePath = downloadedPackage.RootPath", codeBehind);
        Assert.Contains("WriteDesktopAppSettingsAsync", codeBehind);
        Assert.Contains("PopulateSqlInstanceChoices", codeBehind);
        Assert.Contains("PopulateServerIpChoices", codeBehind);
        Assert.Contains("BuildHostingBundleStatusMessage", codeBehind);
        Assert.Contains("IsHostingBundleInstalled", codeBehind);
        Assert.Contains("InstallHostingBundle = InstallHostingBundleBox.IsChecked == true", codeBehind);
        Assert.Contains("DiscoverSqlInstances", codeBehind);
        Assert.Contains("DiscoverServerIps", codeBehind);
        Assert.Contains("NextButton_Click", codeBehind);
        Assert.Contains("BackButton_Click", codeBehind);
        Assert.Contains("StartInstallModeButton_Click", codeBehind);
        Assert.Contains("StartDesktopAppModeButton_Click", codeBehind);
        Assert.Contains("DesktopApp hazırlama modu seçildi", codeBehind);
        Assert.Contains("Server kurulumu çalıştırılmayacak", codeBehind);
        Assert.Contains("DesktopApp'i Hazırla", codeBehind);
        Assert.Contains("Mode = _isUninstallMode ? \"Uninstall\" : (_isDesktopOnlyMode ? \"DesktopApp\" : \"Install\")", codeBehind);
        Assert.Contains("IsCafeOrdersInstalled", codeBehind);
        Assert.Contains("Güncelleme modu", codeBehind);
        Assert.Contains("StartUninstallModeButton_Click", codeBehind);
        Assert.Contains("ExecuteUninstallAsync", codeBehind);
        Assert.Contains("ShowAppDialogAsync", codeBehind);
        Assert.Contains("ValidateCurrentStep", codeBehind);
        Assert.Contains("UpdateStepState", codeBehind);
        Assert.Contains("UpdateLeftPanelModeText", codeBehind);
        Assert.Contains("Kurulumu kaldır", codeBehind);
        Assert.Contains("CopyLogButton_Click", codeBehind);
        Assert.Contains("Clipboard.SetText", codeBehind);
        Assert.Contains("RefreshReviewSummary", codeBehind);
        Assert.Contains("LogTextBlock.Text +=", codeBehind);
        Assert.Contains("LogScrollViewer.ScrollToEnd", codeBehind);
    }

    [Fact]
    public void InstallerScript_UsesProductionPackageAndProtectsMutableData()
    {
        var script = ReadRepoFile("installer", "Install-CafeOrders.ps1");
        var packageScript = ReadRepoFile("installer", "Build-CafeOrders.ProductionPackage.ps1");

        Assert.Contains("https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip", script);
        Assert.Contains("Resolve-PackageRoot", script);
        Assert.Contains("Assert-Package", script);
        Assert.Contains("publishes\\API", script);
        Assert.Contains("publishes\\WebUI", script);
        Assert.Contains("publishes\\AdminAudioAgent", script);
        Assert.Contains("publishes\\ServerNotifier", script);
        Assert.DoesNotContain("\"publishes\\DesktopApp\"", script);
        Assert.Contains("Clear-WebUiDirectoryPreservingUploads", script);
        Assert.Contains("wwwroot", script);
        Assert.Contains("uploads", script);
        Assert.Contains("PreserveUploads", script);
        Assert.Contains("CafeOrders-Production.zip", packageScript);
        Assert.Contains("ZipFile]::CreateFromDirectory", packageScript);
        Assert.Contains("GitHub's normal 100 MB file limit", packageScript);
    }

    [Fact]
    public void InstallerScript_ChecksPrerequisitesIisFirewallTaskAndAcl()
    {
        var script = ReadRepoFile("installer", "Install-CafeOrders.ps1");
        var registerScript = ReadRepoFile("scripts", "Register-CafeOrders.WatchDogTask.ps1");

        Assert.Contains("Assert-Prerequisites", script);
        Assert.Contains("Enable-WindowsFeatureIfNeeded", script);
        Assert.Contains("Enable-WindowsOptionalFeature", script);
        Assert.Contains("Install-WindowsFeature", script);
        Assert.Contains("IIS-WebServerRole", script);
        Assert.Contains("IIS-WebSockets", script);
        Assert.Contains("IIS-ManagementScriptingTools", script);
        Assert.Contains("WebAdministration", script);
        Assert.Contains("Test-HostingBundle", script);
        Assert.Contains("Install-HostingBundleIfNeeded", script);
        Assert.Contains("https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe", script);
        Assert.Contains("/quiet", script);
        Assert.Contains("New-WebAppPool", script);
        Assert.Contains("New-Website", script);
        Assert.Contains("New-NetFirewallRule", script);
        Assert.Contains("Recreating firewall rule", script);
        Assert.Contains("Remove-NetFirewallRule", script);
        Assert.DoesNotContain("AssociatedNetFirewallRule", script);
        Assert.Contains("Protect-ConfigFile", script);
        Assert.Contains("icacls", script);
        Assert.Contains("Register-WatchDogTask", script);
        Assert.Contains("Start-ScheduledTask", script);
        Assert.Contains("-RunLevel Highest", registerScript);
        Assert.Contains("ValidateSet(\"Install\", \"Uninstall\")", script);
        Assert.Contains("Invoke-Uninstall", script);
        Assert.Contains("Remove-CafeOrdersWebsite", script);
        Assert.Contains("Unregister-ScheduledTask", script);
        Assert.Contains("Stop-CafeOrdersProcess", script);
        Assert.Contains("Reset-DirectoryAttributes", script);
        Assert.Contains("Directory removal failed", script);
        Assert.Contains("Directory could not be fully removed", script);
    }

    [Fact]
    public void InstallerScript_WritesEnvironmentSpecificAppSettingsForAllServerComponents()
    {
        var script = ReadRepoFile("installer", "Install-CafeOrders.ps1");

        Assert.Contains("Write-AppSettings", script);
        Assert.Contains("ConnectionStrings", script);
        Assert.Contains("Server=$SqlInstanceName;Database=CafeOrders;User Id=$SqlUser;Password=$SqlPassword", script);
        Assert.Contains("ApiBaseUrl", script);
        Assert.Contains("HubUrl", script);
        Assert.Contains("SharedWebRootPath", script);
        Assert.Contains("CacheDirectory", script);
        Assert.Contains("AdminAudioAgent.log", script);
        Assert.Contains("ServerNotifier.log", script);
        Assert.Contains("DataProtectionKeysPath", script);
        Assert.Contains("CafeOrders.API.log", script);
        Assert.Contains("CafeOrders.WebUI.log", script);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(segments).ToArray()));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CafeOrders.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("CafeOrders repository root could not be resolved.");
    }
}
