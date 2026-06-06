using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using CafeOrders.Application.Contracts.Catalog;
using CafeOrders.Application.Contracts.Devices;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Realtime;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.DesktopApp.Models;
using CafeOrders.DesktopApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CafeOrders.DesktopApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StatusPopupDedupeWindow = TimeSpan.FromSeconds(4);
    private const int StartupRetryCount = 15;
    private static readonly DesktopEndpointOptions EndpointOptions = DesktopEndpointOptions.Load();
    private readonly DateTime _sessionStartedAtUtc = DateTime.UtcNow;

    private readonly DeviceIdentityService _deviceIdentityService = new();
    private readonly HttpClient _httpClient;
    private readonly RealtimeClient _realtimeClient = new();
    private readonly ClientApiService _apiService;
    private readonly DispatcherTimer _statusPopupTimer = new() { Interval = TimeSpan.FromSeconds(3.2) };
    private readonly Queue<(string Title, string Message, string Tone)> _statusPopupQueue = new();
    private readonly Dictionary<string, DateTime> _recentStatusPopupKeys = new();
    private readonly SemaphoreSlim _catalogRefreshGate = new(1, 1);
    private readonly SemaphoreSlim _deviceRefreshGate = new(1, 1);
    private bool _isApprovalPolling;
    private CancellationTokenSource? _completionTransitionCts;
    private CancellationTokenSource? _activeOrderMonitorCts;
    private DateTime _lastAcceptedAtUtc;
    private AppSettingsDto? _currentAppSettings;
    private bool _hasLiveInfoMessageOverride;
    private DeviceRegistrationRequest? _registrationRequest;

    private Guid _deviceId;
    private int? _tableId;
    private string _deviceKey = string.Empty;

    public ObservableCollection<CategoryItem> Categories { get; } = new();
    public ObservableCollection<ProductItem> Products { get; } = new();
    public ObservableCollection<ProductItem> VisibleProducts { get; } = new();
    public ObservableCollection<CartItem> Cart { get; } = new();
    public ObservableCollection<CartItem> ActiveOrderLines { get; } = new();

    [ObservableProperty]
    private string _cafeName = "Cafe Orders";

    [ObservableProperty]
    private string _statusText = "Cihaz kaydi baslatiliyor...";

    [ObservableProperty]
    private string _infoBoxMessage = "Baglanti hazirlaniyor.";

    [ObservableProperty]
    private string _selectedCategoryName = "Tumu";

    [ObservableProperty]
    private bool _isAllCategoriesSelected = true;

    [ObservableProperty]
    private string _displayTableName = "Masa -";

    [ObservableProperty]
    private string _sessionRemainingText = "02:30";

    [ObservableProperty]
    private string _footerCopyrightText = "NightByte Lounge @ 2026 Tum Haklari Saklidir.";

    [ObservableProperty]
    private string _appDeveloperName = "Alperen TEKE";

    [ObservableProperty]
    private string _appDeveloperPhone = "0 (541) 688 88 06";

    [ObservableProperty]
    private string _infoBoxType = "Info";

    [ObservableProperty]
    private string _infoBoxTypeLabel = "Bilgilendirme";

    [ObservableProperty]
    private string _infoBoxIconKey = "campaign";

    [ObservableProperty]
    private string _infoBoxGlyph = "\uE789";

    [ObservableProperty]
    private bool _isApproved;

    [ObservableProperty]
    private bool _isBusy = true;

    [ObservableProperty]
    private bool _isAwaitingApproval = true;

    [ObservableProperty]
    private string _blockingTitle = "Masa Onayi Bekleniyor";

    [ObservableProperty]
    private bool _isMenuViewVisible;

    [ObservableProperty]
    private bool _isOrderStatusVisible;

    [ObservableProperty]
    private int _activeOrderNumber;

    [ObservableProperty]
    private string _activeOrderTitle = "Siparisiniz Alindi";

    [ObservableProperty]
    private string _activeOrderMessage = "Siparisiniz kayda alindi ve mutfaga iletildi.";

    [ObservableProperty]
    private string _activeOrderEtaText = "Hazirlama suresi yogunluga gore degisebilir.";

    [ObservableProperty]
    private string _activeOrderBannerTitle = "Siparisiniz mutfaga iletildi";

    [ObservableProperty]
    private string _activeOrderBannerMessage = "Siparis durumunuz bu ekranda canli guncellenir.";

    [ObservableProperty]
    private string _activeOrderState = "submitted";

    [ObservableProperty]
    private decimal _activeOrderTotal;

    [ObservableProperty]
    private bool _isCartOpen;

    [ObservableProperty]
    private bool _isStatusPopupVisible;

    [ObservableProperty]
    private string _statusPopupTitle = "Bilgilendirme";

    [ObservableProperty]
    private string _statusPopupMessage = string.Empty;

    [ObservableProperty]
    private string _statusPopupTone = "info";

    [ObservableProperty]
    private decimal? _minimumOrderAmount;

    public bool IsBlockingOverlayVisible => IsBusy || IsAwaitingApproval;
    public decimal CartTotal => Cart.Sum(x => x.Total);
    public int CartItemCount => Cart.Sum(x => x.Quantity);
    public bool HasItemsInCart => CartItemCount > 0;
    public bool HasMinimumOrderAmount => MinimumOrderAmount is > 0;
    public bool IsCartBelowMinimum => HasItemsInCart && HasMinimumOrderAmount && CartTotal < MinimumOrderAmount!.Value;
    public bool CanSubmitCart => HasItemsInCart && !IsCartBelowMinimum;
    public decimal MinimumOrderRemaining => IsCartBelowMinimum ? MinimumOrderAmount!.Value - CartTotal : 0m;
    public double MinimumOrderProgressPercent
    {
        get
        {
            if (!HasMinimumOrderAmount || MinimumOrderAmount!.Value <= 0)
            {
                return 100d;
            }

            var percent = (double)(CartTotal / MinimumOrderAmount.Value) * 100d;
            return double.IsFinite(percent) ? Math.Clamp(percent, 0d, 100d) : 0d;
        }
    }

    public GridLength MinimumOrderProgressFill => new(MinimumOrderProgressPercent, GridUnitType.Star);
    public GridLength MinimumOrderProgressRemainder => new(Math.Max(100d - MinimumOrderProgressPercent, 0d), GridUnitType.Star);
    public string MinimumOrderText => HasMinimumOrderAmount
        ? $"Minimum siparis tutari {MinimumOrderAmount!.Value:N0} TL'dir."
        : string.Empty;
    public string MinimumOrderRemainingText => IsCartBelowMinimum
        ? $"Siparis vermek icin {MinimumOrderRemaining:N0} TL daha eklemelisiniz."
        : "Minimum sepet tutari tamamlandi.";
    public bool HasActiveOrder => ActiveOrderLines.Count > 0;
    public TimeSpan AutoCloseAfter => EndpointOptions.AutoCloseAfter;
    public bool IsSessionCountdownVisible => AutoCloseAfter > TimeSpan.Zero;
    public int? CurrentSessionRemainingSeconds => IsSessionCountdownVisible
        ? (int)Math.Ceiling(ResolveCurrentSessionRemaining().TotalSeconds)
        : null;

    public MainViewModel()
    {
        DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl);
        _httpClient = new HttpClient { BaseAddress = new Uri(EndpointOptions.ApiBaseUrl) };
        _apiService = new ClientApiService(_httpClient);
        DesktopAppLogger.Info(
            $"MainViewModel created. ApiBaseUrl={EndpointOptions.ApiBaseUrl}, HubUrl={EndpointOptions.HubUrl}, SharedWebRootPath={EndpointOptions.SharedWebRootPath ?? "-"}, AutoCloseAfterSeconds={EndpointOptions.AutoCloseAfter.TotalSeconds:0}");
        RefreshSessionCountdown();
        Cart.CollectionChanged += OnCartCollectionChanged;
        ActiveOrderLines.CollectionChanged += OnActiveOrderCollectionChanged;
        _statusPopupTimer.Tick += (_, _) =>
        {
            _statusPopupTimer.Stop();
            if (_statusPopupQueue.Count > 0)
            {
                var next = _statusPopupQueue.Dequeue();
                StatusPopupTitle = next.Title;
                StatusPopupMessage = next.Message;
                StatusPopupTone = next.Tone;
                IsStatusPopupVisible = true;
                _statusPopupTimer.Start();
                return;
            }

            IsStatusPopupVisible = false;
        };
    }

    public async Task InitializeAsync()
    {
        var registrationRequest = new DeviceRegistrationRequest(
            _deviceIdentityService.GetHostName(),
            _deviceIdentityService.GetMacAddress(),
            _deviceIdentityService.GetIpAddress(),
            CurrentSessionRemainingSeconds);
        _registrationRequest = registrationRequest;
        DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl, registrationRequest.MacAddress);
        DesktopAppLogger.Info(
            $"Initialize started. HostName={registrationRequest.HostName}, MacAddress={registrationRequest.MacAddress}, IpAddress={registrationRequest.IpAddress}, SessionRemaining={registrationRequest.SessionDurationSeconds}");

        DeviceRegistrationResponse? registration = null;

        for (var attempt = 1; attempt <= StartupRetryCount; attempt++)
        {
            try
            {
                StatusText = $"API baglantisi bekleniyor... deneme {attempt}/{StartupRetryCount}";
                BlockingTitle = "Sunucuya Baglaniliyor";
                DesktopAppLogger.Info($"Device register attempt {attempt}/{StartupRetryCount} started.");
                registration = await _apiService.RegisterAsync(registrationRequest);
                DesktopAppLogger.Info($"Device register attempt {attempt} succeeded. DeviceId={registration?.DeviceId}, IsApproved={registration?.IsApproved}, TableId={registration?.TableId}");
                break;
            }
            catch (HttpRequestException exception)
            {
                DesktopAppLogger.Warning($"Device register attempt {attempt} failed. {exception.Message}");
                if (attempt == StartupRetryCount)
                {
                    StatusText = "API baglantisi kurulamadi. Sunucunun acik oldugunu kontrol edin.";
                    BlockingTitle = "Sunucuya Ulasilamadi";
                    IsBusy = false;
                    OnPropertyChanged(nameof(IsBlockingOverlayVisible));
                    return;
                }

                await Task.Delay(StartupRetryDelay);
            }
        }

        if (registration is null)
        {
            DesktopAppLogger.Warning("Device registration response is null.");
            StatusText = "Kayit basarisiz.";
            IsBusy = false;
            return;
        }

        _deviceId = registration.DeviceId;
        _tableId = registration.TableId;
        _deviceKey = registration.DeviceKey;
        DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl, _deviceKey, _tableId);
        DisplayTableName = _tableId.HasValue ? $"Masa {_tableId.Value:00}" : "Masa -";
        StatusText = registration.Message ?? "Onay bekleniyor.";
        BlockingTitle = registration.IsApproved ? "Hazirlaniyor" : "Masa Onayi Bekleniyor";

        try
        {
            DesktopAppLogger.Info($"Realtime connect starting. HubUrl={EndpointOptions.HubUrl}, DeviceKey={_deviceKey}");
            await _realtimeClient.ConnectAsync(
                EndpointOptions.HubUrl,
                _deviceKey,
                async (_, message, tableId) =>
                {
                    DesktopAppLogger.Info($"Realtime DeviceApproved received. TableId={tableId}, Message={message}");
                    _tableId = tableId;
                    DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl, _deviceKey, _tableId);
                    DisplayTableName = _tableId.HasValue ? $"Masa {_tableId.Value:00}" : "Masa -";
                    StatusText = message ?? "Masa onaylandi.";
                    BlockingTitle = "Sistem Hazirlaniyor";
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadApprovedStateAsync);
                },
                message =>
                {
                    DesktopAppLogger.Warning($"Realtime DeviceRejected received. Message={message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsBusy = false;
                        IsApproved = false;
                        IsAwaitingApproval = true;
                        BlockingTitle = "Cihaz Talebi Reddedildi";
                        StatusText = message;
                        ShowStatusPopup("Cihaz talebi reddedildi", "Lutfen yonetici ile gorusun.", "warning");
                        OnPropertyChanged(nameof(IsBlockingOverlayVisible));
                    });
                },
                (eventName, message) =>
                {
                    DesktopAppLogger.Info($"Realtime order event received. Event={eventName}, Message={message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => HandleRealtimeOrderEvent(eventName, message));
                },
                infoMessage =>
                {
                    DesktopAppLogger.Info($"Realtime info message updated. Type={infoMessage.Type}, Icon={infoMessage.IconKey}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => ApplyInfoMessage(infoMessage));
                },
                settings =>
                {
                    DesktopAppLogger.Info($"Realtime app settings updated. CafeName={settings.CafeName}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => ApplySettingsPresentation(settings));
                },
                () => RefreshCatalogAsync(),
                () => RefreshDeviceRegistrationAsync());
            DesktopAppLogger.Info("Realtime connect completed.");
        }
        catch (Exception exception)
        {
            DesktopAppLogger.Error("Realtime connect failed. Continuing with API polling/register flow.", exception);
        }

        if (registration.IsApproved)
        {
            await LoadApprovedStateAsync();
        }
        else
        {
            IsBusy = false;
            IsAwaitingApproval = true;
            OnPropertyChanged(nameof(IsBlockingOverlayVisible));
            _ = PollApprovalAsync();
        }
    }

    private async Task PollApprovalAsync()
    {
        if (_registrationRequest is null)
        {
            return;
        }

        if (_isApprovalPolling)
        {
            return;
        }

        _isApprovalPolling = true;

        try
        {
            while (!IsApproved)
            {
                await Task.Delay(TimeSpan.FromSeconds(4));

                DeviceRegistrationResponse? registration = null;
                try
                {
                    registration = await _apiService.RegisterAsync(_registrationRequest);
                }
                catch (HttpRequestException exception)
                {
                    DesktopAppLogger.Warning($"Approval polling register failed. {exception.Message}");
                    continue;
                }

                if (registration is null || !registration.IsApproved)
                {
                    continue;
                }

                _deviceId = registration.DeviceId;
                _tableId = registration.TableId;
                _deviceKey = registration.DeviceKey;
                DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl, _deviceKey, _tableId);
                DesktopAppLogger.Info($"Approval polling detected approved device. DeviceId={_deviceId}, TableId={_tableId}");
                DisplayTableName = _tableId.HasValue ? $"Masa {_tableId.Value:00}" : "Masa -";
                StatusText = registration.Message ?? "Masa onaylandi.";
                BlockingTitle = "Sistem Hazirlaniyor";

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadApprovedStateAsync);
                break;
            }
        }
        finally
        {
            _isApprovalPolling = false;
        }
    }

    [RelayCommand]
    private void AddToCart(ProductItem product)
    {
        ShowMenuScreen();
        var current = Cart.FirstOrDefault(x => x.ProductId == product.Id);
        if (current is null)
        {
            current = CreateCartItem(product, 0);
            Cart.Add(current);
        }

        current.Quantity++;
        IsCartOpen = true;
        NotifyCartMetricsChanged();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        item.Quantity++;
        NotifyCartMetricsChanged();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item.Quantity <= 1)
        {
            RemoveCartItem(item);
            return;
        }

        item.Quantity--;
        NotifyCartMetricsChanged();
    }

    [RelayCommand]
    private void RemoveCartItem(CartItem item)
    {
        if (Cart.Remove(item))
        {
            NotifyCartMetricsChanged();
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        NotifyCartMetricsChanged();
    }

    [RelayCommand]
    private void ToggleCart()
    {
        IsCartOpen = !IsCartOpen;
    }

    [RelayCommand]
    private void CloseCart()
    {
        IsCartOpen = false;
    }

    [RelayCommand]
    private void CallSupport()
    {
        ShowStatusPopup("Yardim cagri talebi alindi", "Bir gorevliye haber verildi. Lutfen ekraninizda kalin.", "info");
    }

    [RelayCommand]
    private void SelectCategory(CategoryItem? category)
    {
        SelectedCategoryName = category?.Name ?? "Tumu";
        IsAllCategoriesSelected = category is null;
        foreach (var item in Categories)
        {
            item.IsSelected = category is not null && item.Id == category.Id;
        }
        RefreshVisibleProducts(category?.Id);
    }

    [RelayCommand]
    private void ReturnToMenu()
    {
        ShowMenuScreen();
    }

    [RelayCommand]
    private async Task SubmitOrderAsync()
    {
        if (_deviceId == Guid.Empty || !_tableId.HasValue || Cart.Count == 0)
        {
            return;
        }

        if (IsCartBelowMinimum)
        {
            IsCartOpen = true;
            ShowStatusPopup("Minimum sepet tutari", MinimumOrderRemainingText, "warning");
            return;
        }

        var draftItems = Cart.Select(item => new CartItem
        {
            ProductId = item.ProductId,
            Name = item.Name,
            Description = item.Description,
            ImageUrl = item.ImageUrl,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity
        }).ToArray();

        IsCartOpen = false;
        PreparePendingOrderScreen(draftItems);

        try
        {
            var order = await _apiService.CreateOrderAsync(new CreateOrderRequest(
                _deviceId,
                _tableId.Value,
                Cart.Select(x => new CreateOrderLineRequest(x.ProductId, x.Quantity)).ToArray()));

            ActiveOrderNumber = order.Id;
            ActiveOrderTotal = order.TotalPrice;
            ActiveOrderTitle = "Siparisiniz Alindi";
            ActiveOrderMessage = "Siparisiniz kayda alindi ve mutfaga iletildi.";
            ActiveOrderEtaText = "Hazirlama ve servis durumunu bu ekrandan takip edebilirsiniz.";
            ActiveOrderBannerTitle = "Siparisiniz mutfaga iletildi";
            ActiveOrderBannerMessage = "Siparisiniz admin paneline dustu. Onaylandiginda durum otomatik guncellenecektir.";
            ActiveOrderState = "submitted";
            ShowOrderScreen();
            StartActiveOrderMonitoring(order.Id);
            ShowStatusPopup("Siparisiniz basariyla olusturuldu", "Siparis numaraniz olusturuldu ve mutfaga iletildi.", "success");
        }
        catch (HttpRequestException)
        {
            StopActiveOrderMonitoring();
            ActiveOrderLines.Clear();
            ActiveOrderNumber = 0;
            ActiveOrderTotal = 0;
            ShowMenuScreen();
            ShowStatusPopup("Siparis gonderilemedi", "Sunucuya ulasilamadi. Lutfen tekrar deneyin.", "warning");
            return;
        }
        catch (InvalidOperationException exception)
        {
            StopActiveOrderMonitoring();
            ActiveOrderLines.Clear();
            ActiveOrderNumber = 0;
            ActiveOrderTotal = 0;
            ShowMenuScreen();
            IsCartOpen = true;
            ShowStatusPopup("Siparis gonderilemedi", exception.Message, "warning");
            return;
        }

        Cart.Clear();
        NotifyCartMetricsChanged();
    }

    private async Task LoadApprovedStateAsync()
    {
        DesktopAppLogger.Info("LoadApprovedState started.");
        IsBusy = true;
        IsApproved = true;
        IsAwaitingApproval = false;
        ShowMenuScreen();
        BlockingTitle = "Hazirlaniyor";
        OnPropertyChanged(nameof(IsBlockingOverlayVisible));

        var settings = await _apiService.GetSettingsAsync();
        ApplySettingsPresentation(settings, forceInfoBoxRefresh: true);

        var activeInfoMessage = await _apiService.GetActiveInfoMessageAsync();
        if (activeInfoMessage is not null)
        {
            ApplyInfoMessage(activeInfoMessage);
        }

        var catalog = await _apiService.GetCatalogAsync();
        ApplyCatalog(catalog);

        StatusText = $"{DisplayTableName} aktif.";
        IsBusy = false;
        OnPropertyChanged(nameof(IsBlockingOverlayVisible));
        DesktopAppLogger.Info($"LoadApprovedState completed. Products={Products.Count}, Categories={Categories.Count}");

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                try
                {
                    var response = await _apiService.HeartbeatAsync(new HeartbeatRequest(_deviceId, CurrentSessionRemainingSeconds));
                    if (!response.IsSuccessStatusCode)
                    {
                        DesktopAppLogger.Warning($"Heartbeat failed. StatusCode={(int)response.StatusCode} {response.StatusCode}");
                    }
                }
                catch (Exception exception)
                {
                    DesktopAppLogger.Error("Heartbeat exception.", exception);
                }
            }
        });
    }

    public TimeSpan ResolveCurrentSessionRemaining()
    {
        if (!IsSessionCountdownVisible)
        {
            return TimeSpan.Zero;
        }

        var remaining = _sessionStartedAtUtc.Add(AutoCloseAfter) - DateTime.UtcNow;
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    public void RefreshSessionCountdown()
    {
        var remaining = ResolveCurrentSessionRemaining();
        var totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        SessionRemainingText = $"{totalMinutes:00}:{remaining.Seconds:00}";
        OnPropertyChanged(nameof(CurrentSessionRemainingSeconds));
    }

    private void ApplyCatalog(CatalogResponseDto catalog)
    {
        var selectedCategoryId = IsAllCategoriesSelected
            ? null
            : Categories.FirstOrDefault(x => x.IsSelected)?.Id;
        var catalogVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var categorySortLookup = catalog.Categories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select((category, index) => new { category.Id, Order = index })
            .ToDictionary(x => x.Id, x => x.Order);

        Categories.Clear();
        Products.Clear();

        foreach (var category in catalog.Categories.OrderBy(x => x.SortOrder))
        {
            Categories.Add(new CategoryItem { Id = category.Id, Name = category.Name });
        }

        foreach (var product in catalog.Products
                     .Where(x => x.IsActive)
                     .OrderBy(x => categorySortLookup.TryGetValue(x.CategoryId, out var sortOrder) ? sortOrder : int.MaxValue)
                     .ThenBy(x => x.Name))
        {
            Products.Add(new ProductItem
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = BuildRealtimeImageSource(product.ImageUrl, catalogVersion)
            });
        }

        SyncCartWithCatalog();
        var selectedCategory = selectedCategoryId.HasValue
            ? Categories.FirstOrDefault(x => x.Id == selectedCategoryId.Value)
            : null;
        SelectCategory(selectedCategory);
    }

    private void RefreshVisibleProducts(int? categoryId)
    {
        VisibleProducts.Clear();
        var filtered = categoryId.HasValue
            ? Products.Where(x => x.CategoryId == categoryId.Value)
            : Products;

        foreach (var product in filtered)
        {
            VisibleProducts.Add(product);
        }
    }

    private void HandleRealtimeOrderEvent(string eventName, string message)
    {
        switch (eventName)
        {
            case CafeHubEvents.OrderAccepted:
                _completionTransitionCts?.Cancel();
                _completionTransitionCts = null;
                _lastAcceptedAtUtc = DateTime.UtcNow;
                ApplyAcceptedOrderState(message);
                ShowStatusPopup("Siparis onaylandi", ActiveOrderMessage, "success");
                break;

            case CafeHubEvents.OrderRejected:
                _completionTransitionCts?.Cancel();
                _completionTransitionCts = null;
                ApplyRejectedOrderState(message);
                ShowStatusPopup("Siparis reddedildi", ActiveOrderMessage, "warning");
                break;

            case CafeHubEvents.OrderCompleted:
                if (ActiveOrderState is "submitted" or "submitting")
                {
                    _completionTransitionCts?.Cancel();
                    var cts = new CancellationTokenSource();
                    _completionTransitionCts = cts;
                    _lastAcceptedAtUtc = DateTime.UtcNow;
                    ApplyAcceptedOrderState(_currentAppSettings?.OrderAcceptedMessage ?? "Siparisiniz onaylandi.");
                    ShowStatusPopup("Siparis onaylandi", ActiveOrderMessage, "success");
                    _ = PromoteAcceptedOrderToCompletedAsync(message, cts.Token);
                    return;
                }

                if (ActiveOrderState == "accepted" && DateTime.UtcNow - _lastAcceptedAtUtc < TimeSpan.FromSeconds(4))
                {
                    _completionTransitionCts?.Cancel();
                    var cts = new CancellationTokenSource();
                    _completionTransitionCts = cts;
                    _ = PromoteAcceptedOrderToCompletedAsync(message, cts.Token);
                    return;
                }

                ApplyCompletedOrderState(message);
                ShowStatusPopup("Siparisiniz hazir", ActiveOrderMessage, "success");
                break;
        }
    }

    private void ShowStatusPopup(string title, string message, string tone)
    {
        var popupKey = BuildStatusPopupKey(title, message, tone);
        if (ShouldSkipStatusPopup(popupKey))
        {
            return;
        }

        if (IsStatusPopupVisible)
        {
            if (!_statusPopupQueue.Any(item => BuildStatusPopupKey(item.Title, item.Message, item.Tone) == popupKey))
            {
                _statusPopupQueue.Enqueue((title, message, tone));
            }

            return;
        }

        StatusPopupTitle = title;
        StatusPopupMessage = message;
        StatusPopupTone = tone;
        IsStatusPopupVisible = true;
        _statusPopupTimer.Stop();
        _statusPopupTimer.Start();
    }

    private bool ShouldSkipStatusPopup(string popupKey)
    {
        var now = DateTime.UtcNow;
        foreach (var key in _recentStatusPopupKeys
                     .Where(item => now - item.Value > StatusPopupDedupeWindow)
                     .Select(item => item.Key)
                     .ToList())
        {
            _recentStatusPopupKeys.Remove(key);
        }

        if (_recentStatusPopupKeys.TryGetValue(popupKey, out var lastShownAt) &&
            now - lastShownAt < StatusPopupDedupeWindow)
        {
            return true;
        }

        _recentStatusPopupKeys[popupKey] = now;
        return false;
    }

    private static string BuildStatusPopupKey(string title, string message, string tone)
        => $"{tone.Trim().ToLowerInvariant()}|{title.Trim()}|{message.Trim()}";

    private static string ResolveIconGlyph(string iconKey)
        => iconKey.Trim().ToLowerInvariant() switch
        {
            "error" or "warning" or "priority_high" => "\uE7BA",
            "support" or "support_agent" => "\uE95A",
            "sports_esports" or "stadia_controller" => "\uE7FC",
            "inventory_2" or "inventory" => "\uE719",
            "done" or "check_circle" => "\uE73E",
            _ => "\uE789"
        };

    private void ApplySettingsPresentation(AppSettingsDto settings, bool forceInfoBoxRefresh = false)
    {
        _currentAppSettings = settings;
        CafeName = settings.CafeName;
        AppDeveloperName = settings.AppDeveloperName;
        AppDeveloperPhone = settings.AppDeveloperPhone;
        MinimumOrderAmount = settings.MinimumOrderAmount is > 0 ? settings.MinimumOrderAmount : null;
        FooterCopyrightText = $"{settings.CafeName} @ {DateTime.Now.Year} Tum Haklari Saklidir.";

        if (forceInfoBoxRefresh || !_hasLiveInfoMessageOverride)
        {
            ApplyInfoPresentation(
                string.IsNullOrWhiteSpace(settings.ClientInfoBoxMessage)
                    ? "Siparis ve duyurular burada gorunur."
                    : settings.ClientInfoBoxMessage,
                string.IsNullOrWhiteSpace(settings.ClientInfoBoxType) ? "Info" : settings.ClientInfoBoxType,
                string.IsNullOrWhiteSpace(settings.ClientInfoBoxIcon) ? "campaign" : settings.ClientInfoBoxIcon);
        }
    }

    private void ApplyInfoMessage(InfoMessageDto infoMessage)
    {
        if (!infoMessage.IsActive || string.IsNullOrWhiteSpace(infoMessage.Message))
        {
            _hasLiveInfoMessageOverride = false;
            if (_currentAppSettings is not null)
            {
                ApplySettingsPresentation(_currentAppSettings, forceInfoBoxRefresh: true);
            }

            return;
        }

        _hasLiveInfoMessageOverride = true;
        ApplyInfoPresentation(
            infoMessage.Message,
            string.IsNullOrWhiteSpace(infoMessage.Type) ? "Info" : infoMessage.Type,
            string.IsNullOrWhiteSpace(infoMessage.IconKey) ? "campaign" : infoMessage.IconKey);
    }

    private void ApplyInfoPresentation(string message, string type, string iconKey)
    {
        InfoBoxMessage = message;
        InfoBoxType = type;
        InfoBoxTypeLabel = ResolveInfoTypeLabel(type);
        InfoBoxIconKey = iconKey;
        InfoBoxGlyph = ResolveIconGlyph(InfoBoxIconKey);
    }

    private static string ResolveInfoTypeLabel(string? type)
        => type?.Trim().ToLowerInvariant() switch
        {
            "warning" => "Onemli",
            "success" => "Genel",
            _ => "Duyuru"
        };

    private void PreparePendingOrderScreen(IEnumerable<CartItem> draftItems)
    {
        _completionTransitionCts?.Cancel();
        _completionTransitionCts = null;
        StopActiveOrderMonitoring();
        ActiveOrderLines.Clear();
        foreach (var item in draftItems)
        {
            ActiveOrderLines.Add(item);
        }

        ActiveOrderNumber = 0;
        ActiveOrderTotal = draftItems.Sum(item => item.Total);
        ActiveOrderTitle = "Siparisiniz Olusturuluyor";
        ActiveOrderMessage = "Siparisiniz mutfaga gonderilmek uzere hazirlaniyor.";
        ActiveOrderEtaText = "Baglanti tamamlaninca siparis durum ekraniniz otomatik guncellenecek.";
        ActiveOrderBannerTitle = "Siparis olusturuluyor";
        ActiveOrderBannerMessage = "Lutfen ekraninizda kalin. Islem tamamlandiginda aninda bilgilendirileceksiniz.";
        ActiveOrderState = "submitting";
        ShowOrderScreen();
    }

    private async Task PromoteAcceptedOrderToCompletedAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ApplyCompletedOrderState(message);
            ShowStatusPopup("Siparisiniz hazir", ActiveOrderMessage, "success");
        });
    }

    private void StartActiveOrderMonitoring(int orderId)
    {
        StopActiveOrderMonitoring();
        var cts = new CancellationTokenSource();
        _activeOrderMonitorCts = cts;
        _ = MonitorActiveOrderAsync(orderId, cts.Token);
    }

    private void StopActiveOrderMonitoring()
    {
        _activeOrderMonitorCts?.Cancel();
        _activeOrderMonitorCts = null;
    }

    private async Task MonitorActiveOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                OrderDto? order;
                try
                {
                    order = await _apiService.GetOrderAsync(orderId, cancellationToken);
                }
                catch (HttpRequestException)
                {
                    continue;
                }

                if (order is null)
                {
                    continue;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => ApplyPolledOrderState(order),
                    DispatcherPriority.Background,
                    cancellationToken);

                if (order.Status is "Rejected" or "Completed")
                {
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void ApplyPolledOrderState(OrderDto order)
    {
        if (order.Id != ActiveOrderNumber)
        {
            return;
        }

        switch (order.Status)
        {
            case "Rejected" when ActiveOrderState != "rejected":
                ApplyRejectedOrderState(_currentAppSettings?.OrderRejectedMessage ?? "Siparisiniz su an isleme alinamadi.");
                ShowStatusPopup("Siparis reddedildi", ActiveOrderMessage, "warning");
                break;

            case "Completed" when ActiveOrderState is "submitted" or "submitting":
                _completionTransitionCts?.Cancel();
                var cts = new CancellationTokenSource();
                _completionTransitionCts = cts;
                _lastAcceptedAtUtc = DateTime.UtcNow;
                ApplyAcceptedOrderState(_currentAppSettings?.OrderAcceptedMessage ?? "Siparisiniz onaylandi.");
                ShowStatusPopup("Siparis onaylandi", ActiveOrderMessage, "success");
                _ = PromoteAcceptedOrderToCompletedAsync("Siparisiniz hazir.", cts.Token);
                break;

            case "Completed" when ActiveOrderState != "completed":
                ApplyCompletedOrderState("Siparisiniz hazir.");
                ShowStatusPopup("Siparisiniz hazir", ActiveOrderMessage, "success");
                break;
        }
    }

    private void ApplyAcceptedOrderState(string message)
    {
        ActiveOrderTitle = "Siparisiniz Hazirlaniyor";
        ActiveOrderMessage = string.IsNullOrWhiteSpace(message) ? "Siparisiniz onaylandi." : message;
        ActiveOrderEtaText = "Siparisiniz hazirlaniyor ve kisa sure icinde masaniza servis edilecek.";
        ActiveOrderBannerTitle = "Siparisiniz onaylandi";
        ActiveOrderBannerMessage = "Mutfak ekibi siparisinizi hazirlamaya basladi.";
        ActiveOrderState = "accepted";
        ShowOrderScreen();
    }

    private void ApplyRejectedOrderState(string message)
    {
        ActiveOrderTitle = "Siparisiniz Iptal Edildi";
        ActiveOrderMessage = string.IsNullOrWhiteSpace(message) ? "Siparisiniz su an icin hazirlanamiyor." : message;
        ActiveOrderEtaText = "Yeni bir siparis olusturabilirsiniz.";
        ActiveOrderBannerTitle = "Siparisiniz reddedildi";
        ActiveOrderBannerMessage = "Dilerseniz menuye donerek yeni bir siparis olusturabilirsiniz.";
        ActiveOrderState = "rejected";
        IsCartOpen = false;
        StopActiveOrderMonitoring();
        ShowOrderScreen();
    }

    private void ApplyCompletedOrderState(string message)
    {
        ActiveOrderTitle = "Siparisiniz Hazir";
        ActiveOrderMessage = string.IsNullOrWhiteSpace(message) ? "Siparisiniz tamamlandi ve servis bekliyor." : message;
        ActiveOrderEtaText = "Gorevliler siparisinizi masaniza getirecektir.";
        ActiveOrderBannerTitle = "Siparisiniz hazir";
        ActiveOrderBannerMessage = "Siparis detaylari asagida gorunuyor.";
        ActiveOrderState = "completed";
        StopActiveOrderMonitoring();
        ShowOrderScreen();
    }

    private void ShowMenuScreen()
    {
        IsMenuViewVisible = true;
        IsOrderStatusVisible = false;
        OnPropertyChanged(nameof(IsMenuViewVisible));
        OnPropertyChanged(nameof(IsOrderStatusVisible));
    }

    private void ShowOrderScreen()
    {
        IsMenuViewVisible = false;
        IsOrderStatusVisible = true;
        OnPropertyChanged(nameof(IsMenuViewVisible));
        OnPropertyChanged(nameof(IsOrderStatusVisible));
    }

    private void OnCartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CartItem item in e.NewItems)
            {
                item.PropertyChanged -= CartItemOnPropertyChanged;
                item.PropertyChanged += CartItemOnPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (CartItem item in e.OldItems)
            {
                item.PropertyChanged -= CartItemOnPropertyChanged;
            }
        }

        NotifyCartMetricsChanged();
    }

    private void OnActiveOrderCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasActiveOrder));
    }

    private void CartItemOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CartItem.Quantity) or nameof(CartItem.Total))
        {
            NotifyCartMetricsChanged();
        }
    }

    private void NotifyCartMetricsChanged()
    {
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartItemCount));
        OnPropertyChanged(nameof(HasItemsInCart));
        NotifyMinimumOrderMetricsChanged();
    }

    private void NotifyMinimumOrderMetricsChanged()
    {
        OnPropertyChanged(nameof(HasMinimumOrderAmount));
        OnPropertyChanged(nameof(IsCartBelowMinimum));
        OnPropertyChanged(nameof(CanSubmitCart));
        OnPropertyChanged(nameof(MinimumOrderRemaining));
        OnPropertyChanged(nameof(MinimumOrderProgressPercent));
        OnPropertyChanged(nameof(MinimumOrderProgressFill));
        OnPropertyChanged(nameof(MinimumOrderProgressRemainder));
        OnPropertyChanged(nameof(MinimumOrderText));
        OnPropertyChanged(nameof(MinimumOrderRemainingText));
    }

    private async Task RefreshCatalogAsync()
    {
        if (!IsApproved)
        {
            return;
        }

        await _catalogRefreshGate.WaitAsync();
        try
        {
            CatalogResponseDto catalog;
            try
            {
                catalog = await _apiService.GetCatalogAsync();
            }
            catch (HttpRequestException)
            {
                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ApplyCatalog(catalog));
        }
        finally
        {
            _catalogRefreshGate.Release();
        }
    }

    private async Task RefreshDeviceRegistrationAsync(bool loadApprovedStateWhenNeeded = false)
    {
        if (_registrationRequest is null)
        {
            return;
        }

        await _deviceRefreshGate.WaitAsync();
        try
        {
            DeviceRegistrationResponse? registration;
            try
            {
                registration = await _apiService.RegisterAsync(_registrationRequest);
            }
            catch (HttpRequestException exception)
            {
                DesktopAppLogger.Warning($"Device registration refresh failed. {exception.Message}");
                return;
            }

            if (registration is null)
            {
                return;
            }

            _deviceId = registration.DeviceId;
            _tableId = registration.TableId;
            _deviceKey = registration.DeviceKey;
            DesktopAppLogger.ConfigureRemote(EndpointOptions.ApiBaseUrl, _deviceKey, _tableId);
            DesktopAppLogger.Info($"Device registration refreshed. DeviceId={_deviceId}, Approved={registration.IsApproved}, TableId={_tableId}");
            DisplayTableName = _tableId.HasValue ? $"Masa {_tableId.Value:00}" : "Masa -";

            if (!string.IsNullOrWhiteSpace(registration.Message))
            {
                StatusText = registration.Message;
            }

            if (registration.IsApproved && (!IsApproved || loadApprovedStateWhenNeeded))
            {
                BlockingTitle = "Sistem Hazirlaniyor";
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadApprovedStateAsync);
            }
        }
        finally
        {
            _deviceRefreshGate.Release();
        }
    }

    private void SyncCartWithCatalog()
    {
        if (Cart.Count == 0)
        {
            return;
        }

        var productsById = Products.ToDictionary(product => product.Id);
        var hasChanges = false;

        foreach (var cartItem in Cart)
        {
            if (!productsById.TryGetValue(cartItem.ProductId, out var product))
            {
                continue;
            }

            cartItem.Name = product.Name;
            cartItem.Description = product.Description;
            cartItem.ImageUrl = product.ImageUrl;
            cartItem.UnitPrice = product.Price;
            hasChanges = true;
        }

        if (hasChanges)
        {
            NotifyCartMetricsChanged();
        }
    }

    private static string? BuildRealtimeImageSource(string? imageUrl, string version)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        if (TryBuildSharedMediaUri(imageUrl, version, out var sharedMediaUri))
        {
            return sharedMediaUri;
        }

        if (TryBuildLocalFileUri(imageUrl, version, out var localFileUri))
        {
            return localFileUri;
        }

        var separator = imageUrl.Contains('?') ? "&" : "?";
        return $"{imageUrl}{separator}rt={Uri.EscapeDataString(version)}";
    }

    private static bool TryBuildSharedMediaUri(string imageUrl, string version, out string? sharedMediaUri)
    {
        sharedMediaUri = null;
        if (string.IsNullOrWhiteSpace(EndpointOptions.SharedWebRootPath))
        {
            return false;
        }

        if (!TryExtractUploadsRelativePath(imageUrl, out var relativePath))
        {
            return false;
        }

        var normalizedRelativePath = relativePath
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var sharedFilePath = Path.Combine(EndpointOptions.SharedWebRootPath, normalizedRelativePath);

        try
        {
            if (!File.Exists(sharedFilePath))
            {
                DesktopAppLogger.Warning($"Shared media file not found. Path={sharedFilePath}");
                return false;
            }

            var sharedFileUri = new Uri(sharedFilePath, UriKind.Absolute).AbsoluteUri;
            sharedMediaUri = sharedFileUri;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryBuildLocalFileUri(string imageUrl, string version, out string? localFileUri)
    {
        localFileUri = null;

        if (!Path.IsPathRooted(imageUrl))
        {
            return false;
        }

        try
        {
            localFileUri = File.Exists(imageUrl)
                ? new Uri(imageUrl, UriKind.Absolute).AbsoluteUri
                : null;
            if (localFileUri is null)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExtractUploadsRelativePath(string imageUrl, out string relativePath)
    {
        relativePath = string.Empty;

        string workingPath;
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
        {
            workingPath = absoluteUri.AbsolutePath;
        }
        else
        {
            workingPath = imageUrl;
        }

        var uploadsMarkerIndex = workingPath.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (uploadsMarkerIndex < 0)
        {
            uploadsMarkerIndex = workingPath.IndexOf("\\uploads\\", StringComparison.OrdinalIgnoreCase);
        }

        if (uploadsMarkerIndex < 0)
        {
            return false;
        }

        var uploadsPath = workingPath[uploadsMarkerIndex..]
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var decodedSegments = uploadsPath
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString);

        relativePath = Path.Combine(decodedSegments.ToArray());
        return !string.IsNullOrWhiteSpace(relativePath);
    }

    private static string AppendVersionToken(string source, string version)
    {
        var separator = source.Contains('?') ? "&" : "?";
        return $"{source}{separator}rt={Uri.EscapeDataString(version)}";
    }

    private static CartItem CreateCartItem(ProductItem product, int quantity)
        => new()
        {
            ProductId = product.Id,
            Name = product.Name,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price,
            Quantity = quantity
        };

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsBlockingOverlayVisible));

    partial void OnIsAwaitingApprovalChanged(bool value) => OnPropertyChanged(nameof(IsBlockingOverlayVisible));

    partial void OnMinimumOrderAmountChanged(decimal? value) => NotifyMinimumOrderMetricsChanged();

    public async Task ShutdownAsync()
    {
        StopActiveOrderMonitoring();
        _completionTransitionCts?.Cancel();
        await _realtimeClient.DisconnectAsync();
    }

    private sealed record DesktopEndpointOptions(string ApiBaseUrl, string HubUrl, string? SharedWebRootPath, TimeSpan AutoCloseAfter)
    {
        private const string DefaultApiBaseUrl = "http://localhost:5001/";
        private const string DefaultHubUrl = "http://localhost:5001/hubs/cafe";
        private static readonly TimeSpan DefaultAutoCloseAfter = TimeSpan.FromSeconds(150);

        public static DesktopEndpointOptions Load()
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(appSettingsPath))
            {
                DesktopAppLogger.Warning($"Desktop appsettings not found. Defaults will be used. Path={appSettingsPath}");
                return new DesktopEndpointOptions(DefaultApiBaseUrl, DefaultHubUrl, null, DefaultAutoCloseAfter);
            }

            string rawSettings;
            try
            {
                rawSettings = File.ReadAllText(appSettingsPath);
            }
            catch (Exception exception)
            {
                DesktopAppLogger.Error($"Desktop appsettings could not be read. Path={appSettingsPath}", exception);
                return new DesktopEndpointOptions(DefaultApiBaseUrl, DefaultHubUrl, null, DefaultAutoCloseAfter);
            }

            try
            {
                using var document = JsonDocument.Parse(rawSettings);
                var options = LoadFromJson(document.RootElement);
                DesktopAppLogger.Info($"Desktop appsettings loaded as JSON. Path={appSettingsPath}");
                return options;
            }
            catch (JsonException exception)
            {
                DesktopAppLogger.Error("Desktop appsettings JSON parse failed. Lenient config recovery will be used.", exception);
                return LoadFromLooseText(rawSettings);
            }
            catch (Exception exception)
            {
                DesktopAppLogger.Error("Desktop appsettings load failed. Defaults will be used.", exception);
                return new DesktopEndpointOptions(DefaultApiBaseUrl, DefaultHubUrl, null, DefaultAutoCloseAfter);
            }
        }

        private static DesktopEndpointOptions LoadFromJson(JsonElement rootElement)
        {
            var hasEndpoints = rootElement.TryGetProperty("Endpoints", out var endpointsElement);
            var apiBaseUrl = hasEndpoints && endpointsElement.TryGetProperty("ApiBaseUrl", out var apiElement) && apiElement.ValueKind == JsonValueKind.String
                ? apiElement.GetString()
                : null;
            var hubUrl = hasEndpoints && endpointsElement.TryGetProperty("HubUrl", out var hubElement) && hubElement.ValueKind == JsonValueKind.String
                ? hubElement.GetString()
                : null;
            var sharedWebRootPath = rootElement.TryGetProperty("Media", out var mediaElement) &&
                                    mediaElement.TryGetProperty("SharedWebRootPath", out var sharedRootElement) &&
                                    sharedRootElement.ValueKind == JsonValueKind.String
                ? sharedRootElement.GetString()
                : null;
            var autoCloseAfter = ResolveAutoCloseAfter(rootElement);

            return new DesktopEndpointOptions(
                NormalizeApiBaseUrl(apiBaseUrl),
                NormalizeHubUrl(hubUrl),
                NormalizeSharedWebRootPath(sharedWebRootPath),
                autoCloseAfter);
        }

        private static DesktopEndpointOptions LoadFromLooseText(string rawSettings)
        {
            var apiBaseUrl = ExtractStringValue(rawSettings, "ApiBaseUrl");
            var hubUrl = ExtractStringValue(rawSettings, "HubUrl");
            var sharedWebRootPath = ExtractStringValue(rawSettings, "SharedWebRootPath");
            var autoCloseSeconds = ExtractIntValue(rawSettings, "AutoCloseAfterSeconds");

            var recovered = new DesktopEndpointOptions(
                NormalizeApiBaseUrl(apiBaseUrl),
                NormalizeHubUrl(hubUrl),
                NormalizeSharedWebRootPath(sharedWebRootPath),
                autoCloseSeconds.HasValue ? ResolveAutoCloseAfter(autoCloseSeconds.Value) : DefaultAutoCloseAfter);
            DesktopAppLogger.Warning(
                $"Desktop appsettings recovered from loose text. ApiBaseUrl={recovered.ApiBaseUrl}, HubUrl={recovered.HubUrl}, SharedWebRootPath={recovered.SharedWebRootPath ?? "-"}, AutoCloseAfterSeconds={recovered.AutoCloseAfter.TotalSeconds:0}");
            return recovered;
        }

        private static string NormalizeHubUrl(string? value)
            => string.IsNullOrWhiteSpace(value) ? DefaultHubUrl : value.Trim();

        private static string NormalizeApiBaseUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultApiBaseUrl;
            }

            var trimmed = value.Trim();
            return trimmed.EndsWith("/", StringComparison.Ordinal) ? trimmed : $"{trimmed}/";
        }

        private static string? NormalizeSharedWebRootPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static TimeSpan ResolveAutoCloseAfter(JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty("Session", out var sessionElement) ||
                !sessionElement.TryGetProperty("AutoCloseAfterSeconds", out var secondsElement))
            {
                return DefaultAutoCloseAfter;
            }

            if (!secondsElement.TryGetInt32(out var seconds) || seconds <= 0)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private static TimeSpan ResolveAutoCloseAfter(int seconds)
            => seconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds);

        private static string? ExtractStringValue(string rawSettings, string propertyName)
        {
            var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"";
            var match = Regex.Match(rawSettings, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return null;
            }

            var rawValue = match.Groups["value"].Value;
            try
            {
                return JsonSerializer.Deserialize<string>($"\"{rawValue}\"");
            }
            catch
            {
                return rawValue.Replace("\\\\", "\\").Trim();
            }
        }

        private static int? ExtractIntValue(string rawSettings, string propertyName)
        {
            var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*(?<value>-?\\d+)";
            var match = Regex.Match(rawSettings, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success && int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
    }
}
