using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace CafeOrders.ServerNotifier;

public sealed class ServerNotifierService(NotifierOptions options, NotifierLogger logger) : IAsyncDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(options.ApiBaseUrl)
    };
    private readonly PeriodicTimer _pollTimer = new(TimeSpan.FromSeconds(options.PollIntervalSeconds));
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _runCancellation;

    public event Action<PendingOrdersSnapshot>? SnapshotChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _runCancellation.Token;
        _ = Task.Run(() => RunPollingLoopAsync(token), token);
        _ = Task.Run(() => RunHubLoopAsync(token), token);

        await WaitForApiReadyAsync(token);
        await RefreshPendingOrdersAsync("startup", token);
    }

    public async Task StopAsync()
    {
        _runCancellation?.Cancel();
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _pollTimer.Dispose();
        _httpClient.Dispose();
        _refreshLock.Dispose();
        _runCancellation?.Dispose();
    }

    private async Task RunHubLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(options.HubUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHubHandlers(_hubConnection);
            _hubConnection.Reconnected += async _ =>
            {
                await JoinAdminChannelAsync(cancellationToken);
                await RefreshPendingOrdersAsync("hub-reconnected", cancellationToken);
            };

            await ConnectHubWithRetryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger.Error("ServerNotifier hub loop failed.", exception);
        }
    }

    private void RegisterHubHandlers(HubConnection connection)
    {
        connection.On<OrderDto>(CafeHubEvents.OrderCreated, order => { _ = RefreshPendingOrdersAsync("hub-order-created"); });
        connection.On<JsonElement>(CafeHubEvents.OrderAccepted, payload => { _ = RefreshPendingOrdersAsync("hub-order-accepted"); });
        connection.On<JsonElement>(CafeHubEvents.OrderRejected, payload => { _ = RefreshPendingOrdersAsync("hub-order-rejected"); });
        connection.On<JsonElement>(CafeHubEvents.OrderCompleted, payload => { _ = RefreshPendingOrdersAsync("hub-order-completed"); });
    }

    private async Task ConnectHubWithRetryAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection is null)
        {
            return;
        }

        for (var attempt = 1; attempt <= options.StartupRetryCount && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await _hubConnection.StartAsync(cancellationToken);
                await JoinAdminChannelAsync(cancellationToken);
                logger.Info("ServerNotifier connected to CafeHub admin channel.");
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Warning($"ServerNotifier hub connect attempt {attempt}/{options.StartupRetryCount} failed. {exception.Message}");
                await Task.Delay(TimeSpan.FromSeconds(options.StartupRetryDelaySeconds), cancellationToken);
            }
        }
    }

    private Task JoinAdminChannelAsync(CancellationToken cancellationToken)
        => _hubConnection?.InvokeAsync(CafeHubMethods.JoinAdminChannel, cancellationToken) ?? Task.CompletedTask;

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _pollTimer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshPendingOrdersAsync("poll", cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task WaitForApiReadyAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= options.StartupRetryCount && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(options.BuildApiUri("api/v1/settings/app"), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.Info($"ServerNotifier API readiness confirmed. Attempt={attempt}");
                    return;
                }

                logger.Warning($"ServerNotifier API readiness attempt {attempt}/{options.StartupRetryCount} failed. Status={(int)response.StatusCode}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Warning($"ServerNotifier API readiness attempt {attempt}/{options.StartupRetryCount} failed. {exception.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.StartupRetryDelaySeconds), cancellationToken);
        }
    }

    private async Task RefreshPendingOrdersAsync(string source, CancellationToken cancellationToken = default)
    {
        if (!await _refreshLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var orders = await _httpClient.GetFromJsonAsync<OrderDto[]>(
                options.BuildApiUri("api/v1/orders"),
                _jsonOptions,
                cancellationToken) ?? Array.Empty<OrderDto>();
            var snapshot = PendingOrdersSnapshot.FromOrders(orders);
            logger.Info($"Pending order snapshot refreshed. Source={source}, Count={snapshot.Count}");
            SnapshotChanged?.Invoke(snapshot);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning($"Pending order snapshot refresh failed. Source={source}, Error={exception.Message}");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
