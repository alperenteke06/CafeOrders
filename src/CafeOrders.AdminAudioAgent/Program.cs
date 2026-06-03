using System.Collections.Concurrent;
using CafeOrders.AdminAudioAgent;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

var options = AgentOptions.Load(AppContext.BaseDirectory);
using var httpClient = new HttpClient { BaseAddress = new Uri(EnsureTrailingSlash(options.ApiBaseUrl)) };
var audioService = new AdminAudioService(httpClient, options, new WindowsMediaAudioPlayer(options));
var pendingOrders = new ConcurrentDictionary<int, CancellationTokenSource>();

await using var hubConnection = new HubConnectionBuilder()
    .WithUrl(options.HubUrl)
    .WithAutomaticReconnect()
    .Build();

hubConnection.On<OrderDto>(CafeHubEvents.OrderCreated, order =>
{
    var pending = new CancellationTokenSource();
    if (!pendingOrders.TryAdd(order.Id, pending))
    {
        pending.Dispose();
        return;
    }

    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(Math.Max(0, options.FallbackDelayMilliseconds), pending.Token);
            if (pending.Token.IsCancellationRequested)
            {
                return;
            }

            var played = await audioService.PlayNewOrderSoundAsync(pending.Token);
            if (played && hubConnection.State == HubConnectionState.Connected)
            {
                await hubConnection.InvokeAsync(CafeHubMethods.AcknowledgeOrderSound, order.Id, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (pendingOrders.TryRemove(order.Id, out var removed))
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
        pending.Cancel();
    }
});

await StartAndJoinAsync(hubConnection);
hubConnection.Reconnected += async _ => await JoinAdminChannelAsync(hubConnection);
await Task.Delay(Timeout.InfiniteTimeSpan);

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
