using System.Runtime.InteropServices;
using System.Text;
using NAudio.CoreAudioApi;

namespace CafeOrders.AdminAudioAgent;

public sealed class WindowsMediaAudioPlayer(AgentOptions options, AgentLogger? logger = null) : IAudioPlayer
{
    public Task<bool> PlayAsync(
        string source,
        int? orderId = null,
        CancellationToken cancellationToken = default,
        Func<int?, CancellationToken, Task>? playbackStarted = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Task.FromResult(false);
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.TrySetResult(PlayWithWindowsMediaPlayer(source, orderId, cancellationToken, playbackStarted));
            }
            catch
            {
                completion.TrySetResult(false);
            }
        })
        {
            IsBackground = true,
            Name = "CafeOrders.AdminAudioAgent.Player"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    public Task<bool> PlayFallbackAsync(int? orderId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);

    private bool PlayWithWindowsMediaPlayer(
        string source,
        int? orderId,
        CancellationToken cancellationToken,
        Func<int?, CancellationToken, Task>? playbackStarted)
    {
        var alias = $"CafeOrdersOrderSound{Guid.NewGuid():N}";
        try
        {
            if (!EnsureSystemAudioReady(orderId))
            {
                logger?.Warning($"System audio could not be prepared. MCI playback skipped so WebUI fallback can run. OrderId={FormatOrderId(orderId)}");
                return false;
            }

            if (SendMciCommand($"open \"{source}\" alias {alias}") != 0
                && SendMciCommand($"open \"{source}\" type mpegvideo alias {alias}") != 0)
            {
                logger?.Warning($"MCI could not open order sound. OrderId={FormatOrderId(orderId)}, Source={source}");
                return false;
            }

            SendMciCommand($"setaudio {alias} volume to {Math.Clamp(options.Volume, 0, 100) * 10}");
            if (SendMciCommand($"play {alias}") != 0)
            {
                logger?.Warning($"MCI could not start order sound. OrderId={FormatOrderId(orderId)}, Source={source}");
                return false;
            }
            NotifyPlaybackStarted(playbackStarted, orderId, cancellationToken);

            var stopAt = DateTime.UtcNow.AddSeconds(Math.Max(1, options.MaxPlaybackSeconds));
            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < stopAt)
            {
                var mode = ReadMciStatus(alias, "mode");
                if (mode is "stopped" or "not ready")
                {
                    break;
                }

                Thread.Sleep(150);
            }

            var completed = !cancellationToken.IsCancellationRequested;
            logger?.Info($"MCI order sound playback completed. OrderId={FormatOrderId(orderId)}, Completed={completed}");
            return completed;
        }
        catch (Exception exception)
        {
            logger?.Error($"MCI order sound playback failed. OrderId={FormatOrderId(orderId)}, Source={source}", exception);
            return false;
        }
        finally
        {
            SendMciCommand($"stop {alias}");
            SendMciCommand($"close {alias}");
        }
    }

    private bool EnsureSystemAudioReady(int? orderId)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var endpointVolume = device.AudioEndpointVolume;
            var isMuted = endpointVolume.Mute;
            var currentPercent = (int)Math.Round(endpointVolume.MasterVolumeLevelScalar * 100);
            var targetPercent = Math.Clamp(options.Volume, 1, 100);

            logger?.Info($"PC master endpoint volume check. OrderId={FormatOrderId(orderId)}, Muted={isMuted}, Volume={currentPercent}%, Target={targetPercent}%");

            if (isMuted)
            {
                endpointVolume.Mute = false;
                logger?.Warning($"PC sesi sessizdeydi, AdminAudioAgent tarafindan acildi. OrderId={FormatOrderId(orderId)}");
            }

            if (currentPercent < targetPercent)
            {
                endpointVolume.MasterVolumeLevelScalar = targetPercent / 100f;
                logger?.Info($"PC master endpoint volume AdminAudioAgent tarafindan yukseltildi. OrderId={FormatOrderId(orderId)}, From={currentPercent}%, To={targetPercent}%");
            }

            var verifiedMuted = endpointVolume.Mute;
            var verifiedPercent = (int)Math.Round(endpointVolume.MasterVolumeLevelScalar * 100);
            var isReady = !verifiedMuted && verifiedPercent >= Math.Max(1, Math.Min(targetPercent, 5));
            logger?.Info($"PC master endpoint volume verified. OrderId={FormatOrderId(orderId)}, Muted={verifiedMuted}, Volume={verifiedPercent}%, Ready={isReady}");
            if (!isReady)
            {
                logger?.Warning($"PC master endpoint volume is still not audible after preparation. OrderId={FormatOrderId(orderId)}, Muted={verifiedMuted}, Volume={verifiedPercent}%, Target={targetPercent}%");
            }

            return isReady;
        }
        catch (Exception exception)
        {
            logger?.Warning($"PC master endpoint volume check failed. OrderId={FormatOrderId(orderId)}, Error={exception.Message}");
            return false;
        }
    }

    private static string FormatOrderId(int? orderId) => orderId?.ToString() ?? "(unknown)";

    private void NotifyPlaybackStarted(
        Func<int?, CancellationToken, Task>? playbackStarted,
        int? orderId,
        CancellationToken cancellationToken)
    {
        if (playbackStarted is null)
        {
            return;
        }

        try
        {
            playbackStarted(orderId, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.Warning($"Order sound playback start callback failed. OrderId={FormatOrderId(orderId)}, Error={exception.Message}");
        }
    }

    private static int SendMciCommand(string command)
        => mciSendString(command, null, 0, IntPtr.Zero);

    private static string ReadMciStatus(string alias, string item)
    {
        var buffer = new StringBuilder(128);
        var result = mciSendString($"status {alias} {item}", buffer, buffer.Capacity, IntPtr.Zero);
        return result == 0 ? buffer.ToString().Trim().ToLowerInvariant() : string.Empty;
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr callback);
}
