using System.Runtime.InteropServices;
using System.Text;

namespace CafeOrders.AdminAudioAgent;

public sealed class WindowsMediaAudioPlayer(AgentOptions options, AgentLogger? logger = null) : IAudioPlayer
{
    public Task<bool> PlayAsync(string source, int? orderId = null, CancellationToken cancellationToken = default)
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
                completion.TrySetResult(PlayWithWindowsMediaPlayer(source, orderId, cancellationToken));
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

    private bool PlayWithWindowsMediaPlayer(string source, int? orderId, CancellationToken cancellationToken)
    {
        var alias = $"CafeOrdersOrderSound{Guid.NewGuid():N}";
        try
        {
            EnsureSystemAudioReady(orderId);

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

    private void EnsureSystemAudioReady(int? orderId)
    {
        try
        {
            var getResult = waveOutGetVolume(IntPtr.Zero, out var currentVolume);
            if (getResult != 0)
            {
                logger?.Warning($"Windows wave output volume could not be read. OrderId={FormatOrderId(orderId)}, Result={getResult}");
                return;
            }

            var leftPercent = DecodeWaveVolumePercent(currentVolume & 0xFFFF);
            var rightPercent = DecodeWaveVolumePercent((currentVolume >> 16) & 0xFFFF);
            var currentPercent = Math.Min(leftPercent, rightPercent);
            var targetPercent = Math.Clamp(options.Volume, 1, 100);
            logger?.Info($"Windows wave output volume check. OrderId={FormatOrderId(orderId)}, Left={leftPercent}%, Right={rightPercent}%, Target={targetPercent}%");

            if (currentPercent >= targetPercent)
            {
                return;
            }

            var encodedTarget = EncodeWaveVolumePercent(targetPercent);
            var setResult = waveOutSetVolume(IntPtr.Zero, (uint)(encodedTarget | (encodedTarget << 16)));
            if (setResult == 0)
            {
                logger?.Info($"Windows wave output volume AdminAudioAgent tarafindan yukseltildi. OrderId={FormatOrderId(orderId)}, From={currentPercent}%, To={targetPercent}%");
                return;
            }

            logger?.Warning($"Windows wave output volume could not be changed. OrderId={FormatOrderId(orderId)}, Result={setResult}, From={currentPercent}%, Target={targetPercent}%");
        }
        catch (Exception exception)
        {
            logger?.Warning($"Windows wave output volume check failed. OrderId={FormatOrderId(orderId)}, Error={exception.Message}");
        }
    }

    private static string FormatOrderId(int? orderId) => orderId?.ToString() ?? "(unknown)";

    private static int DecodeWaveVolumePercent(uint value)
        => (int)Math.Round(value / 65535d * 100d);

    private static uint EncodeWaveVolumePercent(int percent)
        => (uint)Math.Round(Math.Clamp(percent, 0, 100) / 100d * 65535d);

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

    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr deviceId, out uint volume);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr deviceId, uint volume);
}
