using System.Text.Json;
using System.IO;

namespace CafeOrders.ServerNotifier;

public sealed record NotifierOptions(
    string ApiBaseUrl,
    string HubUrl,
    string OrdersUrl,
    int PollIntervalSeconds,
    int StartupRetryCount,
    int StartupRetryDelaySeconds,
    string LogPath)
{
    public static NotifierOptions Load(string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        var defaults = new NotifierOptions(
            ApiBaseUrl: "http://192.168.11.24:5001/",
            HubUrl: "http://192.168.11.24:5001/hubs/cafe",
            OrdersUrl: "http://192.168.11.24:5002/?section=orders",
            PollIntervalSeconds: 5,
            StartupRetryCount: 90,
            StartupRetryDelaySeconds: 2,
            LogPath: Path.Combine(baseDirectory, "ServerNotifier.log"));

        var path = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return defaults;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("Notifier", out var notifier))
            {
                return defaults;
            }

            var apiBaseUrl = ReadString(notifier, nameof(ApiBaseUrl), defaults.ApiBaseUrl);
            var hubUrl = ReadString(notifier, nameof(HubUrl), defaults.HubUrl);
            var ordersUrl = ReadString(notifier, nameof(OrdersUrl), defaults.OrdersUrl);
            var pollIntervalSeconds = Math.Clamp(ReadInt(notifier, nameof(PollIntervalSeconds), defaults.PollIntervalSeconds), 2, 300);
            var startupRetryCount = Math.Clamp(ReadInt(notifier, nameof(StartupRetryCount), defaults.StartupRetryCount), 1, 1000);
            var startupRetryDelaySeconds = Math.Clamp(ReadInt(notifier, nameof(StartupRetryDelaySeconds), defaults.StartupRetryDelaySeconds), 1, 60);
            var logPath = ReadString(notifier, nameof(LogPath), defaults.LogPath);
            if (!Path.IsPathRooted(logPath))
            {
                logPath = Path.Combine(baseDirectory, logPath);
            }

            return new NotifierOptions(
                EnsureTrailingSlash(apiBaseUrl),
                hubUrl,
                ordersUrl,
                pollIntervalSeconds,
                startupRetryCount,
                startupRetryDelaySeconds,
                logPath);
        }
        catch
        {
            return defaults;
        }
    }

    public Uri BuildApiUri(string relativePath)
        => new(new Uri(EnsureTrailingSlash(ApiBaseUrl), UriKind.Absolute), relativePath.TrimStart('/'));

    private static string ReadString(JsonElement element, string propertyName, string fallback)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;

    private static int ReadInt(JsonElement element, string propertyName, int fallback)
        => element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : fallback;

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
}
