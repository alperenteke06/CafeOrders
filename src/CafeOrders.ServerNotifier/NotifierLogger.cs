using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Channels;
using CafeOrders.Application.Contracts.Logging;

namespace CafeOrders.ServerNotifier;

public sealed class NotifierLogger(string logPath)
{
    private readonly object _syncRoot = new();
    private readonly Channel<ApplicationLogCreateRequest> _remoteQueue = Channel.CreateBounded<ApplicationLogCreateRequest>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly object _remoteSyncRoot = new();
    private Uri? _remoteEndpoint;
    private int _remoteWorkerStarted;
    private const long MaxLogSizeBytes = 1_000_000;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} {exception.GetType().Name}: {exception.Message}");

    public void ConfigureRemote(string? apiBaseUrl)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl) && Uri.TryCreate(EnsureTrailingSlash(apiBaseUrl), UriKind.Absolute, out var baseUri))
            {
                lock (_remoteSyncRoot)
                {
                    _remoteEndpoint = new Uri(baseUri, "api/v1/logs/client");
                }

                EnsureRemoteWorkerStarted();
            }
        }
        catch
        {
            // Remote logging must never interrupt the notification flow.
        }
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_syncRoot)
            {
                var directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RotateIfNeeded();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logPath, $"[{timestamp}][{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never interrupt the server notifier.
        }

        QueueRemote(level, message);
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(logPath))
        {
            return;
        }

        var fileInfo = new FileInfo(logPath);
        if (fileInfo.Length < MaxLogSizeBytes)
        {
            return;
        }

        var previous = Path.ChangeExtension(logPath, ".previous.log");
        if (File.Exists(previous))
        {
            File.Delete(previous);
        }

        File.Move(logPath, previous);
    }

    private void QueueRemote(string level, string message)
    {
        Uri? endpoint;
        lock (_remoteSyncRoot)
        {
            endpoint = _remoteEndpoint;
        }

        if (endpoint is null)
        {
            return;
        }

        EnsureRemoteWorkerStarted();
        _remoteQueue.Writer.TryWrite(new ApplicationLogCreateRequest(
            "ServerNotifier",
            NormalizeLevel(level),
            message,
            MachineName: Environment.MachineName,
            CreatedAtUtc: DateTime.UtcNow));
    }

    private void EnsureRemoteWorkerStarted()
    {
        if (Interlocked.Exchange(ref _remoteWorkerStarted, 1) == 1)
        {
            return;
        }

        _ = Task.Run(ProcessRemoteQueueAsync);
    }

    private async Task ProcessRemoteQueueAsync()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(4)
        };

        await foreach (var item in _remoteQueue.Reader.ReadAllAsync())
        {
            try
            {
                Uri? endpoint;
                lock (_remoteSyncRoot)
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
                // Local file remains authoritative when the API is unavailable.
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
