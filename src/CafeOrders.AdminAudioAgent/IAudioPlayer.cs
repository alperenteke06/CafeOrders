namespace CafeOrders.AdminAudioAgent;

public interface IAudioPlayer
{
    Task<bool> PlayAsync(
        string source,
        int? orderId = null,
        CancellationToken cancellationToken = default,
        Func<int?, CancellationToken, Task>? playbackStarted = null);

    Task<bool> PlayFallbackAsync(int? orderId = null, CancellationToken cancellationToken = default);
}
