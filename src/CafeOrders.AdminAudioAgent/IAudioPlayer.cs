namespace CafeOrders.AdminAudioAgent;

public interface IAudioPlayer
{
    Task<bool> PlayAsync(string source, int? orderId = null, CancellationToken cancellationToken = default);

    Task<bool> PlayFallbackAsync(int? orderId = null, CancellationToken cancellationToken = default);
}
