using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Threading.Channels;
using CafeOrders.AdminAudioAgent;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

var options = AgentOptions.Load(AppContext.BaseDirectory);
var logger = new AgentLogger(options.LogPath, AppContext.BaseDirectory);
using var httpClient = new HttpClient { BaseAddress = new Uri(EnsureTrailingSlash(options.ApiBaseUrl)) };
var audioService = new AdminAudioService(httpClient, options, new WindowsMediaAudioPlayer(options), logger);
var pendingOrders = new ConcurrentDictionary<int, CancellationTokenSource>();
var queuedOrderIds = new ConcurrentDictionary<int, byte>();
var announcedOrderIds = new ConcurrentDictionary<int, DateTime>();
var playbackQueue = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
{
    SingleReader = true,
    SingleWriter = false
});

await using var hubConnection = new HubConnectionBuilder()
    .WithUrl(options.HubUrl)
    .WithAutomaticReconnect()
    .Build();

_ = Task.Run(() => ProcessPlaybackQueueAsync(playbackQueue.Reader, pendingOrders, queuedOrderIds, audioService, hubConnection, logger));
_ = Task.Run(() => PollPendingOrdersAsync(
    httpClient,
    options,
    pendingOrders,
    queuedOrderIds,
    announcedOrderIds,
    playbackQueue.Writer,
    logger,
    CancellationToken.None));

hubConnection.On<OrderDto>(CafeHubEvents.OrderCreated, order =>
{
    ScheduleFallbackPlayback(
        order.Id,
        "hub",
        options,
        pendingOrders,
        queuedOrderIds,
        announcedOrderIds,
        playbackQueue.Writer,
        logger);
});

hubConnection.On<int>(CafeHubEvents.OrderSoundAcknowledged, orderId =>
{
    announcedOrderIds.TryAdd(orderId, DateTime.UtcNow);
    if (pendingOrders.TryGetValue(orderId, out var pending))
    {
        logger.Info($"Order sound acknowledged. OrderId={orderId}");
        pending.Cancel();
    }

    queuedOrderIds.TryRemove(orderId, out _);
});

logger.Info($"CafeOrders AdminAudioAgent starting. ApiBaseUrl={options.ApiBaseUrl}, HubUrl={options.HubUrl}, WebUiBaseUrl={options.WebUiBaseUrl}, SharedWebRootPath={options.SharedWebRootPath ?? "(empty)"}, LogPath={options.LogPath}");
await StartAndJoinAsync(hubConnection);
hubConnection.Reconnected += async _ => await JoinAdminChannelAsync(hubConnection);
logger.Info("CafeOrders AdminAudioAgent connected.");
await Task.Delay(Timeout.InfiniteTimeSpan);

static async Task ProcessPlaybackQueueAsync(
    ChannelReader<int> queue,
    ConcurrentDictionary<int, CancellationTokenSource> pendingOrders,
    ConcurrentDictionary<int, byte> queuedOrderIds,
    AdminAudioService audioService,
    HubConnection hubConnection,
    AgentLogger logger)
{
    await foreach (var orderId in queue.ReadAllAsync())
    {
        queuedOrderIds.TryRemove(orderId, out _);
        if (!pendingOrders.TryGetValue(orderId, out var pending))
        {
            continue;
        }

        if (pending.Token.IsCancellationRequested)
        {
            if (pendingOrders.TryRemove(orderId, out var cancelled))
            {
                cancelled.Dispose();
            }

            continue;
        }

        try
        {
            logger.Info($"Fallback audio playback started. OrderId={orderId}");
            var played = await audioService.PlayNewOrderSoundAsync(pending.Token);
            if (played && hubConnection.State == HubConnectionState.Connected)
            {
                await hubConnection.InvokeAsync(CafeHubMethods.AcknowledgeOrderSound, orderId, CancellationToken.None);
                logger.Info($"Fallback audio playback acknowledged. OrderId={orderId}");
            }
            else
            {
                logger.Warning($"Fallback audio playback could not be acknowledged. Played={played}, HubState={hubConnection.State}, OrderId={orderId}");
            }
        }
        catch (OperationCanceledException)
        {
            logger.Info($"Fallback audio playback cancelled. OrderId={orderId}");
        }
        catch (Exception exception)
        {
            logger.Error($"Fallback audio playback failed. OrderId={orderId}", exception);
        }
        finally
        {
            if (pendingOrders.TryRemove(orderId, out var removed))
            {
                removed.Dispose();
            }
        }
    }
}

static void ScheduleFallbackPlayback(
    int orderId,
    string source,
    AgentOptions options,
    ConcurrentDictionary<int, CancellationTokenSource> pendingOrders,
    ConcurrentDictionary<int, byte> queuedOrderIds,
    ConcurrentDictionary<int, DateTime> announcedOrderIds,
    ChannelWriter<int> playbackQueue,
    AgentLogger logger)
{
    if (!announcedOrderIds.TryAdd(orderId, DateTime.UtcNow))
    {
        return;
    }

    var pending = new CancellationTokenSource();
    if (!pendingOrders.TryAdd(orderId, pending))
    {
        pending.Dispose();
        return;
    }

    logger.Info($"Order observed for fallback audio. Source={source}, OrderId={orderId}");
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(Math.Max(0, options.FallbackDelayMilliseconds), pending.Token);
            if (pending.Token.IsCancellationRequested)
            {
                return;
            }

            if (queuedOrderIds.TryAdd(orderId, 0))
            {
                await playbackQueue.WriteAsync(orderId, CancellationToken.None);
                logger.Info($"Order queued for fallback audio. Source={source}, OrderId={orderId}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (pending.IsCancellationRequested && pendingOrders.TryRemove(orderId, out var removed))
            {
                removed.Dispose();
            }
        }
    });
}

static async Task PollPendingOrdersAsync(
    HttpClient httpClient,
    AgentOptions options,
    ConcurrentDictionary<int, CancellationTokenSource> pendingOrders,
    ConcurrentDictionary<int, byte> queuedOrderIds,
    ConcurrentDictionary<int, DateTime> announcedOrderIds,
    ChannelWriter<int> playbackQueue,
    AgentLogger logger,
    CancellationToken cancellationToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(1000, options.PollIntervalMilliseconds)));
    while (!cancellationToken.IsCancellationRequested)
    {
        await QueuePendingOrdersFromApiAsync(
            httpClient,
            options,
            pendingOrders,
            queuedOrderIds,
            announcedOrderIds,
            playbackQueue,
            logger,
            cancellationToken);

        await timer.WaitForNextTickAsync(cancellationToken);
    }
}

static async Task QueuePendingOrdersFromApiAsync(
    HttpClient httpClient,
    AgentOptions options,
    ConcurrentDictionary<int, CancellationTokenSource> pendingOrders,
    ConcurrentDictionary<int, byte> queuedOrderIds,
    ConcurrentDictionary<int, DateTime> announcedOrderIds,
    ChannelWriter<int> playbackQueue,
    AgentLogger logger,
    CancellationToken cancellationToken)
{
    try
    {
        var orders = await httpClient.GetFromJsonAsync<IReadOnlyCollection<OrderDto>>("api/v1/orders", cancellationToken)
            ?? Array.Empty<OrderDto>();

        foreach (var order in orders.Where(order => string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase)))
        {
            ScheduleFallbackPlayback(
                order.Id,
                "poll",
                options,
                pendingOrders,
                queuedOrderIds,
                announcedOrderIds,
                playbackQueue,
                logger);
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception exception)
    {
        logger.Warning($"Pending order polling failed: {exception.Message}");
    }
}

static async Task StartAndJoinAsync(HubConnection hubConnection)
{
    while (true)
    {
        try
        {
            await hubConnection.StartAsync();
            await JoinAdminChannelAsync(hubConnection);
            return;
        }
        catch
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

static Task JoinAdminChannelAsync(HubConnection hubConnection)
    => hubConnection.InvokeAsync(CafeHubMethods.JoinAdminChannel);

static string EnsureTrailingSlash(string value)
    => value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
