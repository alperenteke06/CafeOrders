using System.Globalization;

namespace CafeOrders.WebUI.Services;

public static class UploadValidation
{
    public const long MaxProductImageBytes = 20 * 1024 * 1024;
    public const long MaxSoundBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private static readonly HashSet<string> AllowedSoundExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".ogg",
        ".m4a",
        ".aac",
        ".flac",
        ".webm"
    };

    public static bool IsAllowedImage(string? fileName, string? contentType)
        => HasAllowedExtension(fileName, AllowedImageExtensions) ||
           HasExpectedContentType(contentType, "image/");

    public static bool IsAllowedSound(string? fileName, string? contentType)
        => HasAllowedExtension(fileName, AllowedSoundExtensions) ||
           HasExpectedContentType(contentType, "audio/");

    public static string ResolveImageExtension(string? fileName, string? contentType)
    {
        var extension = NormalizeExtension(fileName);
        if (extension is not null && AllowedImageExtensions.Contains(extension))
        {
            return extension;
        }

        return ResolveExtensionFromContentType(contentType, ".png");
    }

    public static string ResolveSoundExtension(string? fileName, string? contentType)
    {
        var extension = NormalizeExtension(fileName);
        if (extension is not null && AllowedSoundExtensions.Contains(extension))
        {
            return extension;
        }

        return ResolveExtensionFromContentType(contentType, ".mp3");
    }

    private static bool HasAllowedExtension(string? fileName, HashSet<string> allowedExtensions)
    {
        var extension = NormalizeExtension(fileName);
        return extension is not null && allowedExtensions.Contains(extension);
    }

    private static string? NormalizeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension)
            ? null
            : extension.ToLower(CultureInfo.InvariantCulture);
    }

    private static bool HasExpectedContentType(string? contentType, string expectedContentTypePrefix)
        => !string.IsNullOrWhiteSpace(contentType) &&
           contentType.StartsWith(expectedContentTypePrefix, StringComparison.OrdinalIgnoreCase);

    private static string ResolveExtensionFromContentType(string? contentType, string fallbackExtension)
    {
        var normalized = contentType?.Trim().ToLower(CultureInfo.InvariantCulture);
        return normalized switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "audio/mpeg" => ".mp3",
            "audio/mp3" => ".mp3",
            "audio/wav" => ".wav",
            "audio/x-wav" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/webm" => ".webm",
            "audio/aac" => ".aac",
            "audio/mp4" => ".m4a",
            _ => fallbackExtension
        };
    }
}
