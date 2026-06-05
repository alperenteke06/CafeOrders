namespace CafeOrders.AdminAudioAgent;

public static class AudioSourceResolver
{
    public static string? Resolve(string? configuredSource, string webUiBaseUrl, string? fallbackSource, string? sharedWebRootPath = null)
    {
        var source = string.IsNullOrWhiteSpace(configuredSource) ? fallbackSource : configuredSource;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        source = source.Trim();
        var localUploadSource = TryResolveFromSharedWebRoot(source, sharedWebRootPath);
        if (!string.IsNullOrWhiteSpace(localUploadSource))
        {
            return localUploadSource;
        }

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

    private static string? TryResolveFromSharedWebRoot(string source, string? sharedWebRootPath)
    {
        if (string.IsNullOrWhiteSpace(sharedWebRootPath))
        {
            return null;
        }

        string? relativePath = null;
        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.IsFile)
            {
                return null;
            }

            if (absoluteUri.Scheme is "http" or "https")
            {
                relativePath = absoluteUri.AbsolutePath;
            }
        }
        else if (!Path.IsPathRooted(source))
        {
            relativePath = source;
        }
        else if (source.StartsWith("/", StringComparison.Ordinal) || source.StartsWith("\\", StringComparison.Ordinal))
        {
            relativePath = source;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var root = Path.GetFullPath(sharedWebRootPath);
        var segments = relativePath
            .TrimStart('/', '\\')
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        if (segments.Length == 0)
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(new[] { root }.Concat(segments).ToArray()));
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!candidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            && !candidate.StartsWith($"{normalizedRoot}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !candidate.StartsWith($"{normalizedRoot}{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(candidate) ? candidate : null;
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
