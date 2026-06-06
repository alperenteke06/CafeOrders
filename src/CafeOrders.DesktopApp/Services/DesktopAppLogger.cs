using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CafeOrders.Application.Contracts.Logging;

namespace CafeOrders.DesktopApp.Services;

public static class DesktopAppLogger
{
    private const long MaxLogSizeBytes = 5 * 1024 * 1024;
    private static readonly object SyncRoot = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "DesktopApp.log");
    private static readonly Channel<ApplicationLogCreateRequest> RemoteQueue = Channel.CreateBounded<ApplicationLogCreateRequest>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
    private static readonly object RemoteSyncRoot = new();
    private static Uri? _remoteEndpoint;
    private static string? _deviceKey;
    private static int? _tableId;
    private static int _remoteWorkerStarted;

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}");

    public static string CurrentLogPath => LogPath;

    public static void ConfigureRemote(string? apiBaseUrl, string? deviceKey = null, int? tableId = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl) && Uri.TryCreate(EnsureTrailingSlash(apiBaseUrl), UriKind.Absolute, out var baseUri))
            {
                lock (RemoteSyncRoot)
                {
                    _remoteEndpoint = new Uri(baseUri, "api/v1/logs/client");
                    _deviceKey = string.IsNullOrWhiteSpace(deviceKey) ? _deviceKey : deviceKey.Trim().ToLowerInvariant();
                    _tableId = tableId ?? _tableId;
                }

                EnsureRemoteWorkerStarted();
            }
        }
        catch
        {
            // Remote logging must never interrupt the kiosk flow.
        }
    }

    private static void Write(string level, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (SyncRoot)
            {
                RotateIfNeeded();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{level}] {message}{Environment.NewLine}");
            }

            QueueRemote(level, message);
        }
        catch
        {
            // Logging must never interrupt the kiosk flow.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
        {
            return;
        }

        var fileInfo = new FileInfo(LogPath);
        if (fileInfo.Length < MaxLogSizeBytes)
        {
            return;
        }

        var archivePath = Path.Combine(fileInfo.DirectoryName!, "DesktopApp.previous.log");
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(LogPath, archivePath);
    }

    private static void QueueRemote(string level, string message)
    {
        Uri? endpoint;
        string? deviceKey;
        int? tableId;
        lock (RemoteSyncRoot)
        {
            endpoint = _remoteEndpoint;
            deviceKey = _deviceKey;
            tableId = _tableId;
        }

        if (endpoint is null)
        {
            return;
        }

        EnsureRemoteWorkerStarted();
        RemoteQueue.Writer.TryWrite(new ApplicationLogCreateRequest(
            "DesktopApp",
            NormalizeLevel(level),
            message,
            MachineName: Environment.MachineName,
            DeviceKey: deviceKey,
            TableId: tableId,
            CreatedAtUtc: DateTime.UtcNow));
    }

    private static void EnsureRemoteWorkerStarted()
    {
        if (Interlocked.Exchange(ref _remoteWorkerStarted, 1) == 1)
        {
            return;
        }

        _ = Task.Run(ProcessRemoteQueueAsync);
    }

    private static async Task ProcessRemoteQueueAsync()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(4)
        };

        await foreach (var item in RemoteQueue.Reader.ReadAllAsync())
        {
            try
            {
                Uri? endpoint;
                lock (RemoteSyncRoot)
                {
                    endpoint = _remoteEndpoint;
                }

                if (endpoint is not null)
                {
                    using var response = await httpClient.PostAsJsonAsync(endpoint, item);
                }
            }
            catch
            {
                // Remote logging is best-effort; local file remains authoritative on the client.
            }
        }
    }

    private static string NormalizeLevel(string level)
        => level switch
        {
            "WARN" => "Warning",
            "ERROR" => "Error",
            _ => "Info"
        };

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value.TrimEnd('/')}/";
}
