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
        object? endpointVolumeObject = null;
        IMMDevice? device = null;
        IMMDeviceEnumerator? enumerator = null;

        try
        {
            enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
            var endpointResult = enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device);
            if (endpointResult != 0 || device is null)
            {
                logger?.Warning($"Default audio endpoint could not be resolved. OrderId={FormatOrderId(orderId)}, HResult={endpointResult}");
                return;
            }

            var endpointVolumeId = typeof(IAudioEndpointVolume).GUID;
            var activateResult = device.Activate(ref endpointVolumeId, ClsCtx.All, IntPtr.Zero, out endpointVolumeObject);
            if (activateResult != 0 || endpointVolumeObject is not IAudioEndpointVolume endpointVolume)
            {
                logger?.Warning($"Default audio endpoint volume could not be activated. OrderId={FormatOrderId(orderId)}, HResult={activateResult}");
                return;
            }

            endpointVolume.GetMute(out var isMuted);
            endpointVolume.GetMasterVolumeLevelScalar(out var currentVolume);
            var currentPercent = (int)Math.Round(currentVolume * 100);
            var targetPercent = Math.Clamp(options.Volume, 1, 100);
            logger?.Info($"PC master volume check. OrderId={FormatOrderId(orderId)}, Muted={isMuted}, Volume={currentPercent}%, Target={targetPercent}%");

            if (isMuted)
            {
                endpointVolume.SetMute(false, Guid.Empty);
                logger?.Warning($"PC sesi kapaliydi, AdminAudioAgent tarafindan acildi. OrderId={FormatOrderId(orderId)}");
            }

            if (currentPercent < targetPercent)
            {
                endpointVolume.SetMasterVolumeLevelScalar(targetPercent / 100f, Guid.Empty);
                logger?.Info($"PC master volume AdminAudioAgent tarafindan yukseltildi. OrderId={FormatOrderId(orderId)}, From={currentPercent}%, To={targetPercent}%");
            }
        }
        catch (Exception exception)
        {
            logger?.Warning($"PC master volume check failed. OrderId={FormatOrderId(orderId)}, Error={exception.Message}");
        }
        finally
        {
            ReleaseComObject(endpointVolumeObject);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
        }
    }

    private static string FormatOrderId(int? orderId) => orderId?.ToString() ?? "(unknown)";

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
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

    private enum EDataFlow
    {
        Render = 0
    }

    private enum ERole
    {
        Multimedia = 1
    }

    [Flags]
    private enum ClsCtx
    {
        All = 23
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumerator
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, ClsCtx clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object endpointVolume);

        [PreserveSig]
        int OpenPropertyStore(uint access, out IntPtr properties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        [PreserveSig]
        int GetState(out uint state);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(IntPtr client);

        [PreserveSig]
        int UnregisterControlChangeNotify(IntPtr client);

        [PreserveSig]
        int GetChannelCount(out uint channelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float level, Guid eventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float level);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float level, Guid eventContext);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float level);

        [PreserveSig]
        int SetChannelVolumeLevel(uint channelNumber, float level, Guid eventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint channelNumber, out float level);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint channelNumber, float level, Guid eventContext);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint channelNumber, out float level);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool isMuted, Guid eventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);

        [PreserveSig]
        int GetVolumeStepInfo(out uint step, out uint stepCount);

        [PreserveSig]
        int VolumeStepUp(Guid eventContext);

        [PreserveSig]
        int VolumeStepDown(Guid eventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint hardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float minDecibels, out float maxDecibels, out float incrementDecibels);
    }
}
