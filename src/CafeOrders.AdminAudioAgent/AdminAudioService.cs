using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using CafeOrders.Application.Contracts.Settings;

namespace CafeOrders.AdminAudioAgent;

public sealed class AdminAudioService(HttpClient httpClient, AgentOptions options, IAudioPlayer audioPlayer, AgentLogger? logger = null)
{
    public HttpClient HttpClient => httpClient;

    public async Task<bool> PlayNewOrderSoundAsync(
        int? orderId = null,
        Func<int?, CancellationToken, Task>? playbackStarted = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await httpClient.GetFromJsonAsync<AppSettingsDto>("api/v1/settings/app", cancellationToken);
        if (settings is null || !settings.EnableNewOrderSound)
        {
            logger?.Warning("New order sound is disabled or settings could not be loaded.");
            return false;
        }

        var source = AudioSourceResolver.Resolve(settings.NewOrderSoundUrl, options.WebUiBaseUrl, options.FallbackSoundPath, options.SharedWebRootPath);
        if (string.IsNullOrWhiteSpace(source))
        {
            logger?.Warning("New order sound source is empty. Playback skipped.");
            return options.UseSystemBeepFallback && await audioPlayer.PlayFallbackAsync(orderId, cancellationToken);
        }

        try
        {
            var localSource = await ResolveLocalSourceAsync(source, orderId, cancellationToken);
            logger?.Info($"Playing new order sound. OrderId={FormatOrderId(orderId)}, Source={localSource}");
            return await audioPlayer.PlayAsync(localSource, orderId, cancellationToken, playbackStarted);
        }
        catch (Exception exception)
        {
            logger?.Error("New order sound playback failed.", exception);
            return options.UseSystemBeepFallback && await audioPlayer.PlayFallbackAsync(orderId, cancellationToken);
        }
    }

    private static string FormatOrderId(int? orderId) => orderId?.ToString() ?? "(unknown)";

    private async Task<string> ResolveLocalSourceAsync(string source, int? orderId, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.IsFile)
        {
            return source;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return source;
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp3";
        }

        Exception? lastException = null;
        foreach (var cacheDirectory in ResolveCacheDirectories())
        {
            try
            {
                Directory.CreateDirectory(cacheDirectory);
                CleanupOldCacheFiles(cacheDirectory);

                var localPath = Path.Combine(cacheDirectory, BuildCacheFileName(uri, extension, orderId));
                await using var remoteStream = await httpClient.GetStreamAsync(uri, cancellationToken);
                await using var fileStream = new FileStream(
                    localPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);
                await remoteStream.CopyToAsync(fileStream, cancellationToken);
                return localPath;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
            {
                lastException = exception;
                logger?.Warning($"New order sound cache write failed. Directory={cacheDirectory}, OrderId={FormatOrderId(orderId)}, Error={exception.Message}");
            }
        }

        throw new IOException("New order sound could not be cached to any writable directory.", lastException);
    }

    private IEnumerable<string> ResolveCacheDirectories()
    {
        if (!string.IsNullOrWhiteSpace(options.CacheDirectory))
        {
            yield return options.CacheDirectory;
        }

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CafeOrders",
            "AdminAudioAgent",
            "cache");
        yield return Path.Combine(AppContext.BaseDirectory, "cache");
        yield return Path.Combine(Path.GetTempPath(), "CafeOrders", "AdminAudioAgent", "cache");
    }

    private static string BuildCacheFileName(Uri source, string extension, int? orderId)
    {
        var sourceHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source.ToString())))[..16].ToLowerInvariant();
        var orderToken = orderId?.ToString() ?? "unknown";
        return $"new-order-{orderToken}-{sourceHash}-{Guid.NewGuid():N}{extension}";
    }

    private void CleanupOldCacheFiles(string cacheDirectory)
    {
        try
        {
            var threshold = DateTime.UtcNow.AddDays(-1);
            foreach (var file in Directory.EnumerateFiles(cacheDirectory, "new-order-*"))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < threshold)
                {
                    info.Delete();
                }
            }
        }
        catch (Exception exception)
        {
            logger?.Warning($"New order sound cache cleanup skipped. Directory={cacheDirectory}, Error={exception.Message}");
        }
    }
}
