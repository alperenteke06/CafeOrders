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
                    "FallbackDelayMilliseconds": 900,
                    "Volume": 75,
                    "UseSystemBeepFallback": false
                  }
                }
                """);

            var options = AgentOptions.Load(directory);

            Assert.Equal("http://192.168.11.24:5001/", options.ApiBaseUrl);
            Assert.Equal("http://192.168.11.24:5001/hubs/cafe", options.HubUrl);
            Assert.Equal("http://192.168.11.24:5002/", options.WebUiBaseUrl);
            Assert.Equal(900, options.FallbackDelayMilliseconds);
            Assert.Equal(75, options.Volume);
            Assert.False(options.UseSystemBeepFallback);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
