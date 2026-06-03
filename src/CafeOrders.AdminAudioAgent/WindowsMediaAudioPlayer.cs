using System.Runtime.Versioning;

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

    public Task<bool> PlayFallbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            for (var index = 0; index < 3 && !cancellationToken.IsCancellationRequested; index++)
            {
                Console.Beep(880, 160);
                Thread.Sleep(90);
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    [SupportedOSPlatform("windows")]
    private bool PlayWithWindowsMediaPlayer(string source, CancellationToken cancellationToken)
    {
        var playerType = Type.GetTypeFromProgID("WMPlayer.OCX");
        if (playerType is null)
        {
            return false;
        }

        dynamic? player = Activator.CreateInstance(playerType);
        if (player is null)
        {
            return false;
        }

        try
        {
            player.settings.volume = options.Volume;
            player.URL = source;
            player.controls.play();

            var startedAt = DateTime.UtcNow;
            var stopAt = startedAt.AddSeconds(Math.Max(1, options.MaxPlaybackSeconds));
            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < stopAt)
            {
                var state = (int)player.playState;
                if (DateTime.UtcNow - startedAt > TimeSpan.FromMilliseconds(700) && state is 1 or 8 or 10)
                {
                    break;
                }

                Thread.Sleep(150);
            }

            player.controls.stop();
            return true;
        }
        finally
        {
            try
            {
                player.close();
            }
            catch
            {
            }
        }
    }
}
