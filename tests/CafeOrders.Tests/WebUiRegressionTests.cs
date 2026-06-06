namespace CafeOrders.Tests;

public sealed class WebUiRegressionTests
{
    [Fact]
    public void NewOrderSoundScript_QueuesPlaybackForBackgroundTabsAndGestureUnlock()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("queueNewOrderSound(order)", index);
        Assert.Contains("scheduleWebUiFallbackIfAgentSilent(orderId)", index);
        Assert.Contains("connection.on('OrderSoundPlaybackFailed'", index);
        Assert.Contains("queueNewOrderSoundById(orderId, 'agent-failed')", index);
        Assert.Contains("newOrderSoundQueue", index);
        Assert.Contains("drainNewOrderSoundQueue", index);
        Assert.Contains("waitForAudioEnd", index);
        Assert.Contains("pendingNewOrderSound", index);
        Assert.Contains("prepareNewOrderSoundGuard", index);
        Assert.Contains("visibilitychange", index);
        Assert.Contains("pointerdown", index);
        Assert.Contains("playFallbackOrderBeep", index);
        Assert.Contains("canUseWebUiNewOrderSound", index);
        Assert.Contains("ReportOrderSoundPlaybackStarted", index);
        Assert.Contains("ReportOrderSoundPlaybackStarted', Number(orderId), 'WebUI'", index);
        Assert.Contains("connection.on('OrderSoundPlaybackStarted'", index);
        Assert.Contains("markNewOrderSoundHandledElsewhere", index);
        Assert.Contains("externallyHandledNewOrderSoundIds", index);
        Assert.Contains("cafeordersaudiohandled", index);
        Assert.Contains("completion === 'ended'", index);
        Assert.Contains("AcknowledgeOrderSound", index);
        Assert.Contains("AcknowledgeOrderSound', Number(orderId), 'WebUI'", index);
        Assert.Contains("OrderSoundAcknowledged", index);
        Assert.DoesNotContain("appendNotificationFromOrderCreated(order);\r\n                queueNewOrderSound(order);", index);
    }

    [Fact]
    public void PlaybackStartedSignal_IsBroadcastOnlyToOtherAdminClients()
    {
        var hub = ReadRepoFile("src", "CafeOrders.Infrastructure", "Realtime", "CafeHub.cs");

        Assert.Contains("ReportOrderSoundPlaybackStarted", hub);
        Assert.Contains("ReportOrderSoundPlaybackFailed", hub);
        Assert.Contains("Clients.OthersInGroup(\"admin\")", hub);
        Assert.DoesNotContain("Clients.Group(\"admin\").SendAsync(CafeOrders.Application.Contracts.Realtime.CafeHubEvents.OrderSoundPlaybackStarted", hub);
    }

    [Fact]
    public void DevConnectionStrings_UseLocalSqlInstanceInsteadOfIpAddress()
    {
        var apiSettings = ReadRepoFile("src", "CafeOrders.API", "appsettings.json");
        var webSettings = ReadRepoFile("src", "CafeOrders.WebUI", "appsettings.json");
        var dependencyInjection = ReadRepoFile("src", "CafeOrders.Infrastructure", "DependencyInjection.cs");
        var dbContextFactory = ReadRepoFile("src", "CafeOrders.Infrastructure", "Persistence", "CafeOrdersDbContextFactory.cs");

        Assert.Contains("Server=.\\\\SQLEXPRESS", apiSettings);
        Assert.Contains("Server=.\\\\SQLEXPRESS", webSettings);
        Assert.Contains("Server=.\\\\SQLEXPRESS", dependencyInjection);
        Assert.Contains("BuildConfiguration", dbContextFactory);
        Assert.Contains("GetConnectionString(\"CafeOrders\")", dbContextFactory);
        Assert.Contains("AddEnvironmentVariables", dbContextFactory);
        Assert.DoesNotContain("Server=192.168.11.24\\\\SQLEXPRESS", apiSettings);
        Assert.DoesNotContain("Server=192.168.11.24\\\\SQLEXPRESS", webSettings);
        Assert.DoesNotContain("Server=192.168.11.24\\\\SQLEXPRESS", dependencyInjection);
        Assert.DoesNotContain("Server=192.168.11.24\\\\SQLEXPRESS", dbContextFactory);
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
    public void DesktopCartItemLayout_KeepsQuantityStepperAndPriceInsideDrawer()
    {
        var xaml = ReadRepoFile("src", "CafeOrders.DesktopApp", "MainWindow.xaml");

        Assert.Contains("<ColumnDefinition Width=\"132\" />", xaml);
        Assert.Contains("Text=\"{Binding Total, StringFormat={}{0:N2} TL}\"", xaml);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", xaml);
        Assert.DoesNotContain("Width=\"166\"\r\n                                                            Height=\"38\"", xaml);
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
        Assert.Contains("AppContext.BaseDirectory", logger);
        Assert.Contains("MaxLogSizeBytes", logger);
        Assert.Contains("Logging must never interrupt the kiosk flow.", logger);
    }

    [Fact]
    public void ApiWebUiAndDesktop_LogToApplicationDirectories()
    {
        var apiProgram = ReadRepoFile("src", "CafeOrders.API", "Program.cs");
        var webProgram = ReadRepoFile("src", "CafeOrders.WebUI", "Program.cs");
        var apiSettings = ReadRepoFile("src", "CafeOrders.API", "appsettings.json");
        var webSettings = ReadRepoFile("src", "CafeOrders.WebUI", "appsettings.json");
        var fileLogger = ReadRepoFile("src", "CafeOrders.Infrastructure", "Logging", "LocalFileLogger.cs");

        Assert.Contains("AddLocalFile(builder.Configuration, \"CafeOrders.API.log\")", apiProgram);
        Assert.Contains("AddLocalFile(builder.Configuration, \"CafeOrders.WebUI.log\")", webProgram);
        Assert.Contains("\"FilePath\": \"CafeOrders.API.log\"", apiSettings);
        Assert.Contains("\"FilePath\": \"CafeOrders.WebUI.log\"", webSettings);
        Assert.Contains("AppContext.BaseDirectory", fileLogger);
        Assert.Contains("previous", fileLogger);
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

    [Fact]
    public void MinimumOrderAmount_IsManagedRealtimeAndRenderedInDesktopCart()
    {
        var settingsSection = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_SettingsSection.cshtml");
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var xaml = ReadRepoFile("src", "CafeOrders.DesktopApp", "MainWindow.xaml");
        var viewModel = ReadRepoFile("src", "CafeOrders.DesktopApp", "ViewModels", "MainViewModel.cs");
        var realtimeClient = ReadRepoFile("src", "CafeOrders.DesktopApp", "Services", "RealtimeClient.cs");
        var appSettingsDto = ReadRepoFile("src", "CafeOrders.Application", "Contracts", "Settings", "AppSettingsDto.cs");

        Assert.Contains("minimumOrderAmount", index);
        Assert.Contains("parseOptionalDecimal", index);
        Assert.Contains("Minimum Sepet Tutari", settingsSection);
        Assert.Contains("MinimumOrderAmount", appSettingsDto);
        Assert.Contains("AppSettingsUpdated", realtimeClient);
        Assert.Contains("MinimumOrderAmount = settings.MinimumOrderAmount", viewModel);
        Assert.Contains("IsCartBelowMinimum", xaml);
        Assert.Contains("CartMinimumProgressBarStyle", xaml);
        Assert.Contains("CanSubmitCart", xaml);
    }

    [Fact]
    public void WebUiRealtimeRefresh_DoesNotReloadEverySectionOrOpenEditors()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var css = ReadRepoFile("src", "CafeOrders.WebUI", "wwwroot", "css", "site.css");

        Assert.Contains("const defaultRealtimeRefreshSections = ['dashboard', 'orders', 'devices', 'notifications']", index);
        Assert.Contains("requestSnapshotRefresh(['dashboard', 'orders', 'notifications'])", index);
        Assert.Contains("requestSnapshotRefresh(['dashboard', 'products', 'categories'])", index);
        Assert.Contains("refreshSnapshot(true, { realtime: true })", index);
        Assert.Contains("prepareRealtimeSectionHtml", index);
        Assert.Contains("canAutoRefreshSection(targetSection, refreshSections)", index);
        Assert.Contains("isAutoRefreshBlockedForUserInput", index);
        Assert.Contains("['INPUT', 'TEXTAREA', 'SELECT'].includes(activeElement.tagName)", index);
        Assert.Contains("#productEditorModal:not([hidden])", index);
        Assert.Contains("#quickPriceModal:not([hidden])", index);
        Assert.Contains("#sectionHost.is-realtime-refresh", css);
        Assert.Contains("animation: none !important", css);
    }

    [Fact]
    public void WebUiNumberInputs_UseCustomThemeStylingInsteadOfNativeSpinners()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var categories = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_CategoriesSection.cshtml");
        var settings = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_SettingsSection.cshtml");
        var css = ReadRepoFile("src", "CafeOrders.WebUI", "wwwroot", "css", "site.css");

        Assert.Contains("id=\"productEditorPrice\" class=\"modal-input\" type=\"number\"", index);
        Assert.Contains("class=\"quick-price-input\" type=\"number\"", index);
        Assert.Contains("id=\"categorySortOrder\" type=\"number\"", categories);
        Assert.Contains("id=\"minimumOrderAmount\"", settings);
        Assert.Contains("input[type=\"number\"]::-webkit-inner-spin-button", css);
        Assert.Contains("-moz-appearance: textfield", css);
        Assert.Contains(".field-row input[type=\"number\"]", css);
        Assert.Contains(".quick-price-input-shell:focus-within", css);
    }

    [Fact]
    public void WebUiSearchBox_CanBeClearedAndDoesNotCarryQueryAcrossSections()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var css = ReadRepoFile("src", "CafeOrders.WebUI", "wwwroot", "css", "site.css");

        Assert.Contains("adminSearchClearButton", index);
        Assert.Contains("clearSearchBox", index);
        Assert.Contains("syncSearchClearButton", index);
        Assert.Contains("normalizeSectionOverrides", index);
        Assert.Contains("normalized.search = null", index);
        Assert.Contains("normalized.category = 'all'", index);
        Assert.Contains(".topbar-search-clear", css);
    }

    [Fact]
    public void SystemLogsSection_IsReachableFilteredAndRealtime()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");
        var logsSection = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_LogsSection.cshtml");
        var css = ReadRepoFile("src", "CafeOrders.WebUI", "wwwroot", "css", "site.css");

        Assert.Contains("data-section=\"logs\"", index);
        Assert.Contains("Sistem Loglari", index);
        Assert.Contains("connection.on('ApplicationLogCreated'", index);
        Assert.Contains("appendApplicationLogEntry(log)", index);
        Assert.Contains("logMatchesActiveFilters", index);
        Assert.Contains("getLogSourceFilter", index);
        Assert.Contains("getLogLevelFilter", index);
        Assert.Contains("applicationLogStream", logsSection);
        Assert.Contains("log-filter-chip", logsSection);
        Assert.Contains("loadSection('logs'", logsSection);
        Assert.Contains(".log-terminal-shell", css);
        Assert.Contains(".terminal-scanline", css);
        Assert.Contains(".log-entry-row", css);
    }

    [Fact]
    public void ApplicationLogs_AreStoredFromServerAndClientSources()
    {
        var apiProgram = ReadRepoFile("src", "CafeOrders.API", "Program.cs");
        var webProgram = ReadRepoFile("src", "CafeOrders.WebUI", "Program.cs");
        var logsController = ReadRepoFile("src", "CafeOrders.API", "Controllers", "LogsController.cs");
        var desktopLogger = ReadRepoFile("src", "CafeOrders.DesktopApp", "Services", "DesktopAppLogger.cs");
        var agentLogger = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "AgentLogger.cs");
        var migration = ReadRepoFile("src", "CafeOrders.Infrastructure", "Persistence", "Migrations", "20260606120000_AddApplicationLogEntries.cs");

        Assert.Contains("AddApplicationLogQueue(builder.Configuration, \"API\"", apiProgram);
        Assert.Contains("AddApplicationLogQueue(builder.Configuration, \"WebUI\"", webProgram);
        Assert.Contains("[Route(\"api/v1/logs\")]", logsController);
        Assert.Contains("[HttpPost(\"client\")]", logsController);
        Assert.Contains("ConfigureRemote", desktopLogger);
        Assert.Contains("api/v1/logs/client", desktopLogger);
        Assert.Contains("Channel.CreateBounded<ApplicationLogCreateRequest>", desktopLogger);
        Assert.Contains("ConfigureRemote", agentLogger);
        Assert.Contains("api/v1/logs/client", agentLogger);
        Assert.Contains("OrderId=", agentLogger);
        Assert.Contains("CreateTable", migration);
        Assert.Contains("ApplicationLogEntries", migration);
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
