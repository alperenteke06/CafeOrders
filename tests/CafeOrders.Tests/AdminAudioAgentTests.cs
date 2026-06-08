using CafeOrders.AdminAudioAgent;

namespace CafeOrders.Tests;

public sealed class AdminAudioAgentTests
{
    [Fact]
    public void AudioSourceResolver_ResolvesRelativeUploadAgainstWebUiBaseUrl()
    {
        var result = AudioSourceResolver.Resolve(
            "/uploads/sounds/new-order.mp3",
            "http://192.168.11.24:5002/",
            fallbackSource: null);

        Assert.Equal("http://192.168.11.24:5002/uploads/sounds/new-order.mp3", result);
    }

    [Fact]
    public void AudioSourceResolver_ResolvesRelativeUploadFromSharedWebRootWhenAvailable()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "wwwroot");
        var soundsDirectory = Path.Combine(webRoot, "uploads", "sounds");
        Directory.CreateDirectory(soundsDirectory);
        var soundPath = Path.Combine(soundsDirectory, "new order.mp3");
        File.WriteAllText(soundPath, "fake sound");

        try
        {
            var result = AudioSourceResolver.Resolve(
                "/uploads/sounds/new%20order.mp3",
                "http://192.168.11.24:5002/",
                fallbackSource: null,
                sharedWebRootPath: webRoot);

            Assert.Equal(soundPath, result);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(webRoot)!, recursive: true);
        }
    }

    [Fact]
    public void AudioSourceResolver_ResolvesAbsoluteWebUiUploadFromSharedWebRootWhenAvailable()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "wwwroot");
        var soundsDirectory = Path.Combine(webRoot, "uploads", "sounds");
        Directory.CreateDirectory(soundsDirectory);
        var soundPath = Path.Combine(soundsDirectory, "new-order.mp3");
        File.WriteAllText(soundPath, "fake sound");

        try
        {
            var result = AudioSourceResolver.Resolve(
                "http://192.168.11.24:5002/uploads/sounds/new-order.mp3",
                "http://192.168.11.24:5002/",
                fallbackSource: null,
                sharedWebRootPath: webRoot);

            Assert.Equal(soundPath, result);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(webRoot)!, recursive: true);
        }
    }

    [Fact]
    public void AudioSourceResolver_UsesFallbackWhenConfiguredSourceIsEmpty()
    {
        var result = AudioSourceResolver.Resolve(
            "",
            "http://192.168.11.24:5002/",
            @"C:\CafeOrders\sounds\fallback.wav");

        Assert.Equal(@"C:\CafeOrders\sounds\fallback.wav", result);
    }

    [Fact]
    public void AgentOptions_Load_ReadsDeploymentDefaultsFromAppSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), """
                {
                  "Agent": {
                    "ApiBaseUrl": "http://192.168.11.24:5001/",
                    "HubUrl": "http://192.168.11.24:5001/hubs/cafe",
                    "WebUiBaseUrl": "http://192.168.11.24:5002/",
                    "SharedWebRootPath": "C:\\inetpub\\wwwroot\\WebUI\\wwwroot",
                    "LogPath": "C:\\Temp\\CafeOrders.AdminAudioAgent.log",
                    "FallbackDelayMilliseconds": 900,
                    "PollIntervalMilliseconds": 1500,
                    "Volume": 75,
                    "UseSystemBeepFallback": false
                  }
                }
                """);

            var options = AgentOptions.Load(directory);

            Assert.Equal("http://192.168.11.24:5001/", options.ApiBaseUrl);
            Assert.Equal("http://192.168.11.24:5001/hubs/cafe", options.HubUrl);
            Assert.Equal("http://192.168.11.24:5002/", options.WebUiBaseUrl);
            Assert.Equal(@"C:\inetpub\wwwroot\WebUI\wwwroot", options.SharedWebRootPath);
            Assert.Equal(@"C:\Temp\CafeOrders.AdminAudioAgent.log", options.LogPath);
            Assert.Equal(900, options.FallbackDelayMilliseconds);
            Assert.Equal(1500, options.PollIntervalMilliseconds);
            Assert.Equal(75, options.Volume);
            Assert.False(options.UseSystemBeepFallback);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AgentProgram_UsesSingleReaderPlaybackQueueForBurstOrders()
    {
        var program = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "Program.cs");

        Assert.Contains("Channel.CreateUnbounded<int>", program);
        Assert.Contains("SingleReader = true", program);
        Assert.Contains("ProcessPlaybackQueueAsync", program);
        Assert.Contains("ReadAllAsync", program);
        Assert.Contains("OrderSoundPlaybackStarted", program);
        Assert.Contains("ReportAgentPlaybackStartedAsync", program);
        Assert.Contains("ReportAgentPlaybackFailedAsync", program);
        Assert.Contains("CafeHubMethods.ReportOrderSoundPlaybackStarted", program);
        Assert.Contains("CafeHubMethods.ReportOrderSoundPlaybackFailed", program);
        Assert.Contains("OrderSoundAcknowledged", program);
        Assert.Contains("\"AdminAudioAgent\"", program);
    }

    [Fact]
    public void AgentProgram_PollsApiForUnplayedOrdersWhenHubEventIsMissed()
    {
        var program = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "Program.cs");
        var options = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "AgentOptions.cs");

        Assert.Contains("PollPendingOrdersAsync", program);
        Assert.Contains("QueuePendingOrdersFromApiAsync", program);
        Assert.Contains("api/v1/orders?soundPendingOnly=true", program);
        Assert.Contains("api/v1/orders/{orderId}/sound-played", program);
        Assert.Contains("MarkOrderSoundPlayedAsync", program);
        Assert.Contains("webPlaybackStartedAt", program);
        Assert.Contains("MaxPlaybackSeconds", program);
        Assert.Contains("Pending sound poll returned unplayed orders", program);
        Assert.Contains("Pending sound poll scheduling completed", program);
        Assert.Contains("static bool ScheduleFallbackPlayback", program);
        Assert.DoesNotContain("order.Status, \"Pending\"", program);
        Assert.DoesNotContain("StringComparison.OrdinalIgnoreCase) &&\r\n                     !order.IsSoundPlayed", program);
        Assert.Contains("!order.IsSoundPlayed", program);
        Assert.Contains("\"poll\"", program);
        Assert.DoesNotContain("announcedOrderIds", program);
        Assert.Contains("PollIntervalMilliseconds", options);
    }

    [Fact]
    public void AgentDefaults_PlayConfiguredSoundImmediatelyWithoutSystemBeep()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = AgentOptions.Load(directory);
        var player = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "WindowsMediaAudioPlayer.cs");

        Assert.Equal(0, options.FallbackDelayMilliseconds);
        Assert.Equal(Path.Combine(directory, "AdminAudioAgent.log"), options.LogPath);
        Assert.False(options.UseSystemBeepFallback);
        Assert.Contains("mciSendString", player);
        Assert.Contains("NAudio.CoreAudioApi", player);
        Assert.Contains("GetDefaultAudioEndpoint", player);
        Assert.Contains("endpointVolume.Mute = false", player);
        Assert.Contains("MasterVolumeLevelScalar", player);
        Assert.Contains("PC master endpoint volume verified", player);
        Assert.Contains("System audio could not be prepared", player);
        Assert.DoesNotContain("IAudioEndpointVolume", player);
        Assert.Contains("OrderId=", player);
        Assert.DoesNotContain("Console.Beep", player);
    }

    [Fact]
    public void AgentReportsPlaybackStartedOnlyAfterAudioPlaybackStarts()
    {
        var program = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "Program.cs");
        var service = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "AdminAudioService.cs");
        var player = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "WindowsMediaAudioPlayer.cs");
        var audioPlayer = ReadRepoFile("src", "CafeOrders.AdminAudioAgent", "IAudioPlayer.cs");

        Assert.Contains("Func<int?, CancellationToken, Task>? playbackStarted", audioPlayer);
        Assert.Contains("playbackStarted", service);
        Assert.Contains("NotifyPlaybackStarted(playbackStarted", player);
        Assert.DoesNotContain("await ReportAgentPlaybackStartedAsync(hubConnection, orderId, logger, CancellationToken.None);", program);
        Assert.Contains("await ReportAgentPlaybackStartedAsync(hubConnection, value, logger, cancellationToken);", program);
    }

    [Fact]
    public void WatchDogScript_StartsAdminAudioAgentWhenMissing()
    {
        var script = ReadRepoFile("scripts", "CafeOrders.WatchDog.ps1");
        var hiddenRunner = ReadRepoFile("scripts", "Run-CafeOrders.WatchDogHidden.vbs");
        var registerScript = ReadRepoFile("scripts", "Register-CafeOrders.WatchDogTask.ps1");

        Assert.Contains("Ensure-AdminAudioAgentRunning", script);
        Assert.Contains("Start-Process -FilePath $resolvedPath", script);
        Assert.Contains(@"C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe", script);
        Assert.Contains("-AdminAudioAgentPath", hiddenRunner);
        Assert.Contains("AdminAudioAgentPath", registerScript);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(segments).ToArray()));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CafeOrders.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("CafeOrders repository root could not be resolved.");
    }
}
