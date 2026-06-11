namespace CafeOrders.Tests;

public sealed class ServerNotifierTests
{
    [Fact]
    public void ServerNotifierProject_IsIncludedInSolutionAndConfiguredForWpf()
    {
        var solution = ReadRepoFile("CafeOrders.slnx");
        var project = ReadRepoFile("src", "CafeOrders.ServerNotifier", "CafeOrders.ServerNotifier.csproj");
        var appSettings = ReadRepoFile("src", "CafeOrders.ServerNotifier", "appsettings.json");

        Assert.Contains("src/CafeOrders.ServerNotifier/CafeOrders.ServerNotifier.csproj", solution);
        Assert.Contains("<UseWPF>true</UseWPF>", project);
        Assert.Contains("Microsoft.AspNetCore.SignalR.Client", project);
        Assert.Contains("\"HubUrl\": \"http://192.168.11.24:5001/hubs/cafe\"", appSettings);
        Assert.Contains("\"OrdersUrl\": \"http://192.168.11.24:5002/?section=orders\"", appSettings);
    }

    [Fact]
    public void ServerNotifier_UsesRealtimeAdminChannelAndSnapshotPolling()
    {
        var service = ReadRepoFile("src", "CafeOrders.ServerNotifier", "ServerNotifierService.cs");
        var snapshot = ReadRepoFile("src", "CafeOrders.ServerNotifier", "PendingOrdersSnapshot.cs");

        Assert.Contains("WithAutomaticReconnect", service);
        Assert.Contains("JoinAdminChannel", service);
        Assert.Contains("CafeHubEvents.OrderCreated", service);
        Assert.Contains("CafeHubEvents.OrderAccepted", service);
        Assert.Contains("CafeHubEvents.OrderRejected", service);
        Assert.Contains("CafeHubEvents.OrderCompleted", service);
        Assert.Contains("PeriodicTimer", service);
        Assert.Contains("api/v1/orders", service);
        Assert.Contains("Status, \"Pending\"", snapshot);
        Assert.Contains("Masa {order.TableId:00}", snapshot);
    }

    [Fact]
    public void ServerNotifierWindow_IsTopMostBottomRightAndHasNoCloseButton()
    {
        var xaml = ReadRepoFile("src", "CafeOrders.ServerNotifier", "MainWindow.xaml");
        var codeBehind = ReadRepoFile("src", "CafeOrders.ServerNotifier", "MainWindow.xaml.cs");

        Assert.Contains("Topmost=\"True\"", xaml);
        Assert.Contains("ShowInTaskbar=\"False\"", xaml);
        Assert.Contains("ShowActivated=\"False\"", xaml);
        Assert.Contains("WindowStyle=\"None\"", xaml);
        Assert.Contains("ResizeMode=\"NoResize\"", xaml);
        Assert.Contains("SystemParameters.WorkArea", codeBehind);
        Assert.Contains("area.Right - Width - 18", codeBehind);
        Assert.Contains("area.Bottom - Height - 18", codeBehind);
        Assert.DoesNotContain("Activate();", codeBehind);
        Assert.DoesNotContain("Kapat", xaml);
        Assert.DoesNotContain("Close", xaml);
    }

    [Fact]
    public void ServerNotifier_LogsLocallyAndPublishesToCentralLogPanel()
    {
        var logger = ReadRepoFile("src", "CafeOrders.ServerNotifier", "NotifierLogger.cs");
        var window = ReadRepoFile("src", "CafeOrders.ServerNotifier", "MainWindow.xaml.cs");
        var options = ReadRepoFile("src", "CafeOrders.ServerNotifier", "NotifierOptions.cs");

        Assert.Contains("ServerNotifier.log", options);
        Assert.Contains("Path.Combine(baseDirectory, \"ServerNotifier.log\")", options);
        Assert.Contains("ConfigureRemote(_options.ApiBaseUrl)", window);
        Assert.Contains("Channel.CreateBounded<ApplicationLogCreateRequest>", logger);
        Assert.Contains("api/v1/logs/client", logger);
        Assert.Contains("\"ServerNotifier\"", logger);
        Assert.Contains("Local file remains authoritative when the API is unavailable", logger);
    }

    [Fact]
    public void WatchDog_StartsServerNotifierWhenMissing()
    {
        var script = ReadRepoFile("scripts", "CafeOrders.WatchDog.ps1");
        var hiddenRunner = ReadRepoFile("scripts", "Run-CafeOrders.WatchDogHidden.vbs");
        var registerScript = ReadRepoFile("scripts", "Register-CafeOrders.WatchDogTask.ps1");
        var readme = ReadRepoFile("scripts", "CafeOrders.WatchDog.README.md");

        Assert.Contains("ServerNotifierPath", script);
        Assert.Contains("Ensure-ServerNotifierRunning", script);
        Assert.Contains(@"C:\ServerNotifier\CafeOrders.ServerNotifier.exe", script);
        Assert.Contains("Starting ServerNotifier", script);
        Assert.Contains("-ServerNotifierPath", hiddenRunner);
        Assert.Contains("serverNotifierPath", hiddenRunner);
        Assert.Contains("ServerNotifierPath", registerScript);
        Assert.Contains("CafeOrders.ServerNotifier.exe", readme);
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
