namespace CafeOrders.AdminAudioAgent;

public interface IAudioPlayer
{
    Task<bool> PlayAsync(string source, CancellationToken cancellationToken = default);

    Task<bool> PlayFallbackAsync(CancellationToken cancellationToken = default);
}
