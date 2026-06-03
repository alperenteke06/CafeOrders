namespace CafeOrders.AdminAudioAgent;

public static class AudioSourceResolver
{
    public static string? Resolve(string? configuredSource, string webUiBaseUrl, string? fallbackSource)
    {
        var source = string.IsNullOrWhiteSpace(configuredSource) ? fallbackSource : configuredSource;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        source = source.Trim();
        if (source.StartsWith("/", StringComparison.Ordinal))
        {
            return BuildFromWebUiBase(source, webUiBaseUrl);
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.IsFile ? absoluteUri.LocalPath : absoluteUri.ToString();
        }

        if (Path.IsPathRooted(source))
        {
            return source;
        }

        return BuildFromWebUiBase(source, webUiBaseUrl) ?? source;
    }

    private static string? BuildFromWebUiBase(string source, string webUiBaseUrl)
    {
        if (!Uri.TryCreate(EnsureTrailingSlash(webUiBaseUrl), UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return new Uri(baseUri, source.TrimStart('/')).ToString();
    }

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
}
