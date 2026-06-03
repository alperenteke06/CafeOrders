using System.Collections.Concurrent;
using System.Threading.Channels;
using CafeOrders.AdminAudioAgent;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

var options = AgentOptions.Load(AppContext.BaseDirectory);
var logger = new AgentLogger(options.LogPath);
using var httpClient = new HttpClient { BaseAddress = new Uri(EnsureTrailingSlash(options.ApiBaseUrl)) };
var audioService = new AdminAudioService(httpClient, options, new WindowsMediaAudioPlayer(options), logger);
var pendingOrders = new ConcurrentDictionary<int, CancellationTokenSource>();
var queuedOrderIds = new ConcurrentDictionary<int, byte>();
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

hubConnection.On<OrderDto>(CafeHubEvents.OrderCreated, order =>
{
    var pending = new CancellationTokenSource();
    if (!pendingOrders.TryAdd(order.Id, pending))
    {
        pending.Dispose();
        return;
    }

    logger.Info($"OrderCreated received. OrderId={order.Id}");
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(Math.Max(0, options.FallbackDelayMilliseconds), pending.Token);
            if (pending.Token.IsCancellationRequested)
            {
                return;
            }

            if (queuedOrderIds.TryAdd(order.Id, 0))
            {
                await playbackQueue.Writer.WriteAsync(order.Id, CancellationToken.None);
                logger.Info($"Order queued for fallback audio. OrderId={order.Id}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (pending.IsCancellationRequested && pendingOrders.TryRemove(order.Id, out var removed))
            {
                removed.Dispose();
            }
        }
    });
});

hubConnection.On<int>(CafeHubEvents.OrderSoundAcknowledged, orderId =>
{
    if (pendingOrders.TryGetValue(orderId, out var pending))
    {
        logger.Info($"Order sound acknowledged. OrderId={orderId}");
        pending.Cancel();
    }

    queuedOrderIds.TryRemove(orderId, out _);
});

logger.Info("CafeOrders AdminAudioAgent starting.");
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
