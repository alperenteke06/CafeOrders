using System.Net.Http;
using System.Net.Http.Json;
using CafeOrders.Application.Contracts.Settings;

namespace CafeOrders.AdminAudioAgent;

public sealed class AdminAudioService(HttpClient httpClient, AgentOptions options, IAudioPlayer audioPlayer, AgentLogger? logger = null)
{
    public HttpClient HttpClient => httpClient;

    public async Task<bool> PlayNewOrderSoundAsync(int? orderId = null, CancellationToken cancellationToken = default)
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
            var localSource = await ResolveLocalSourceAsync(source, cancellationToken);
            logger?.Info($"Playing new order sound. OrderId={FormatOrderId(orderId)}, Source={localSource}");
            return await audioPlayer.PlayAsync(localSource, orderId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger?.Error("New order sound playback failed.", exception);
            return options.UseSystemBeepFallback && await audioPlayer.PlayFallbackAsync(orderId, cancellationToken);
        }
    }

    private static string FormatOrderId(int? orderId) => orderId?.ToString() ?? "(unknown)";

    private async Task<string> ResolveLocalSourceAsync(string source, CancellationToken cancellationToken)
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

        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CafeOrders",
            "AdminAudioAgent",
            "cache");
        Directory.CreateDirectory(cacheDirectory);

        var localPath = Path.Combine(cacheDirectory, $"new-order{extension}");
        await using var remoteStream = await httpClient.GetStreamAsync(uri, cancellationToken);
        await using var fileStream = File.Create(localPath);
        await remoteStream.CopyToAsync(fileStream, cancellationToken);
        return localPath;
    }
}
