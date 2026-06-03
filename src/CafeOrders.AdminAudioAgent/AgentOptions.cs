using System.Text.Json;

namespace CafeOrders.AdminAudioAgent;

public sealed record AgentOptions(
    string ApiBaseUrl,
    string HubUrl,
    string WebUiBaseUrl,
    string? FallbackSoundPath,
    int FallbackDelayMilliseconds,
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
            1800,
            12,
            90,
            true);

        var path = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return defaults;
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("Agent", out var agent))
        {
            return defaults;
        }

        return defaults with
        {
            ApiBaseUrl = ReadString(agent, nameof(ApiBaseUrl), defaults.ApiBaseUrl),
            HubUrl = ReadString(agent, nameof(HubUrl), defaults.HubUrl),
            WebUiBaseUrl = ReadString(agent, nameof(WebUiBaseUrl), defaults.WebUiBaseUrl),
            FallbackSoundPath = ReadNullableString(agent, nameof(FallbackSoundPath)),
            FallbackDelayMilliseconds = ReadInt(agent, nameof(FallbackDelayMilliseconds), defaults.FallbackDelayMilliseconds),
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
}
