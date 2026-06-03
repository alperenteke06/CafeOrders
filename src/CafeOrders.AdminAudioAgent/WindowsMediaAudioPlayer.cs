using System.Runtime.InteropServices;
using System.Text;

namespace CafeOrders.AdminAudioAgent;

public sealed class WindowsMediaAudioPlayer(AgentOptions options) : IAudioPlayer
{
    public Task<bool> PlayAsync(string source, CancellationToken cancellationToken = default)
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
                completion.TrySetResult(PlayWithWindowsMediaPlayer(source, cancellationToken));
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

    public Task<bool> PlayFallbackAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    private bool PlayWithWindowsMediaPlayer(string source, CancellationToken cancellationToken)
    {
        var alias = $"CafeOrdersOrderSound{Guid.NewGuid():N}";
        try
        {
            if (SendMciCommand($"open \"{source}\" alias {alias}") != 0
                && SendMciCommand($"open \"{source}\" type mpegvideo alias {alias}") != 0)
            {
                return false;
            }

            SendMciCommand($"setaudio {alias} volume to {Math.Clamp(options.Volume, 0, 100) * 10}");
            if (SendMciCommand($"play {alias}") != 0)
            {
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

            return !cancellationToken.IsCancellationRequested;
        }
        catch
        {
            return false;
        }
        finally
        {
            SendMciCommand($"stop {alias}");
            SendMciCommand($"close {alias}");
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
