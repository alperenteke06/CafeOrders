using System.Text.Json;

namespace CafeOrders.AdminAudioAgent;

public sealed record AgentOptions(
    string ApiBaseUrl,
    string HubUrl,
    string WebUiBaseUrl,
    string? SharedWebRootPath,
    string? FallbackSoundPath,
    string LogPath,
    int FallbackDelayMilliseconds,
    int PollIntervalMilliseconds,
    int MaxPlaybackSeconds,
    int Volume,
    bool UseSystemBeepFallback)
{
    public static AgentOptions Load(string baseDirectory)
    {
        var defaults = new AgentOptions(
            "http://localhost:5001/",
            "http://localhost:5001/hubs/cafe",
            "http://localhost:5002/",
            null,
            null,
            Path.Combine(baseDirectory, "AdminAudioAgent.log"),
            0,
            2000,
            12,
            90,
            false);

        var path = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return defaults;
        }

        JsonDocument document;
        try
        {
            using var stream = File.OpenRead(path);
            document = JsonDocument.Parse(stream);
        }
        catch
        {
            return defaults;
        }

        using var _ = document;
        if (!document.RootElement.TryGetProperty("Agent", out var agent))
        {
            return defaults;
        }

        return defaults with
        {
            ApiBaseUrl = ReadString(agent, nameof(ApiBaseUrl), defaults.ApiBaseUrl),
            HubUrl = ReadString(agent, nameof(HubUrl), defaults.HubUrl),
            WebUiBaseUrl = ReadString(agent, nameof(WebUiBaseUrl), defaults.WebUiBaseUrl),
            SharedWebRootPath = ReadNullableString(agent, nameof(SharedWebRootPath)),
            FallbackSoundPath = ReadNullableString(agent, nameof(FallbackSoundPath)),
            LogPath = ResolvePath(baseDirectory, ReadString(agent, nameof(LogPath), defaults.LogPath)),
            FallbackDelayMilliseconds = ReadInt(agent, nameof(FallbackDelayMilliseconds), defaults.FallbackDelayMilliseconds),
            PollIntervalMilliseconds = ReadInt(agent, nameof(PollIntervalMilliseconds), defaults.PollIntervalMilliseconds),
            MaxPlaybackSeconds = ReadInt(agent, nameof(MaxPlaybackSeconds), defaults.MaxPlaybackSeconds),
            Volume = Math.Clamp(ReadInt(agent, nameof(Volume), defaults.Volume), 0, 100),
            UseSystemBeepFallback = ReadBool(agent, nameof(UseSystemBeepFallback), defaults.UseSystemBeepFallback)
        };
    }

    private static string ReadString(JsonElement element, string name, string fallback)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!.Trim()
            : fallback;

    private static string? ReadNullableString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!.Trim()
            : null;

    private static int ReadInt(JsonElement element, string name, int fallback)
        => element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value)
            ? value
            : fallback;

    private static bool ReadBool(JsonElement element, string name, bool fallback)
        => element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : fallback;

    private static string ResolvePath(string baseDirectory, string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
}
