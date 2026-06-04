namespace CafeOrders.Tests;

public sealed class WebUiRegressionTests
{
    [Fact]
    public void NewOrderSoundScript_QueuesPlaybackForBackgroundTabsAndGestureUnlock()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("queueNewOrderSound(order)", index);
        Assert.Contains("newOrderSoundQueue", index);
        Assert.Contains("drainNewOrderSoundQueue", index);
        Assert.Contains("waitForAudioEnd", index);
        Assert.Contains("pendingNewOrderSound", index);
        Assert.Contains("prepareNewOrderSoundGuard", index);
        Assert.Contains("visibilitychange", index);
        Assert.Contains("pointerdown", index);
        Assert.Contains("playFallbackOrderBeep", index);
        Assert.Contains("AcknowledgeOrderSound", index);
        Assert.Contains("OrderSoundAcknowledged", index);
    }

    [Fact]
    public void DevicesSection_HidesOfflineSessionLabelAndRunsClientCountdown()
    {
        var devices = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_DevicesSection.cshtml");
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("ShouldShowSessionRemaining(device)", devices);
        Assert.Contains("device-session-remaining", devices);
        Assert.Contains("startDeviceSessionCountdowns", index);
        Assert.Contains("formatSessionRemaining", index);
    }

    [Fact]
    public void UploadScripts_SupportDropFileAndWebUrlFlows()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("handleProductDrop", index);
        Assert.Contains("handleProductFileChange", index);
        Assert.Contains("updateProductPreviewFromUrl", index);
        Assert.Contains("handleSoundDrop", index);
        Assert.Contains("handleSoundFileChange", index);
        Assert.Contains("updateSoundPreviewFromUrl", index);
        Assert.Contains("normalizeMediaUrl", index);
    }

    [Fact]
    public void DevicesAndCategoriesSections_UseCustomPagination()
    {
        var devices = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_DevicesSection.cshtml");
        var categories = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_CategoriesSection.cshtml");

        Assert.Contains("pagination-pillbar", devices);
        Assert.Contains("loadSection('devices'", devices);
        Assert.Contains("pagination-pillbar", categories);
        Assert.Contains("loadSection('categories'", categories);
    }

    [Fact]
    public void DesktopAppShell_ShowsSessionCountdownInHeader()
    {
        var xaml = ReadRepoFile("src", "CafeOrders.DesktopApp", "MainWindow.xaml");
        var appSettings = ReadRepoFile("src", "CafeOrders.DesktopApp", "appsettings.json");

        Assert.Contains("KALAN SURE", xaml);
        Assert.Contains("SessionRemainingText", xaml);
        Assert.Contains("IsSessionCountdownVisible", xaml);
        Assert.Contains("AutoCloseAfterSeconds", appSettings);
    }

    [Fact]
    public void DesktopAppConfigLoader_RecoversFromBrokenMediaConfigAndLogsDiagnostics()
    {
        var viewModel = ReadRepoFile("src", "CafeOrders.DesktopApp", "ViewModels", "MainViewModel.cs");
        var logger = ReadRepoFile("src", "CafeOrders.DesktopApp", "Services", "DesktopAppLogger.cs");

        Assert.Contains("JsonException", viewModel);
        Assert.Contains("LoadFromLooseText", viewModel);
        Assert.Contains("ExtractStringValue", viewModel);
        Assert.Contains("SharedWebRootPath", viewModel);
        Assert.Contains("Desktop appsettings recovered from loose text", viewModel);
        Assert.Contains("Realtime connect failed. Continuing with API polling/register flow.", viewModel);
        Assert.Contains("DesktopApp.log", logger);
        Assert.Contains("MaxLogSizeBytes", logger);
        Assert.Contains("Logging must never interrupt the kiosk flow.", logger);
    }

    [Fact]
    public void ToastFlows_DedupeManualActionsAndRealtimeEvents()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var viewModel = ReadRepoFile("src", "CafeOrders.DesktopApp", "ViewModels", "MainViewModel.cs");

        Assert.Contains("recentToastKeys", index);
        Assert.Contains("suppressedHubToastKeys", index);
        Assert.Contains("suppressHubToast(`order:${orderId}:accepted`)", index);
        Assert.Contains("consumeSuppressedHubToast(toastKey)", index);
        Assert.Contains("showToast(message, type, dedupeKey = null)", index);
        Assert.Contains("StatusPopupDedupeWindow", viewModel);
        Assert.Contains("ShouldSkipStatusPopup", viewModel);
        Assert.Contains("BuildStatusPopupKey", viewModel);
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
