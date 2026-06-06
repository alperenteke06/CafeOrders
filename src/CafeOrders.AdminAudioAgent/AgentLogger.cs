using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CafeOrders.Application.Contracts.Logging;

namespace CafeOrders.AdminAudioAgent;

public sealed class AgentLogger(string logPath, string? fallbackDirectory = null)
{
    private readonly object _syncRoot = new();
    private readonly string _logPath = string.IsNullOrWhiteSpace(logPath)
        ? Path.Combine(string.IsNullOrWhiteSpace(fallbackDirectory) ? AppContext.BaseDirectory : fallbackDirectory, "AdminAudioAgent.log")
        : logPath;
    private readonly string _fallbackLogPath = Path.Combine(
        string.IsNullOrWhiteSpace(fallbackDirectory) ? AppContext.BaseDirectory : fallbackDirectory,
        "AdminAudioAgent.log");
    private readonly Channel<ApplicationLogCreateRequest> _remoteQueue = Channel.CreateBounded<ApplicationLogCreateRequest>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly object _remoteSyncRoot = new();
    private Uri? _remoteEndpoint;
    private int _remoteWorkerStarted;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} {exception.Message}");

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
            // Remote logging should never stop the fallback sound agent.
        }
    }

    private void Write(string level, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}] {message}{Environment.NewLine}";
            lock (_syncRoot)
            {
                WriteLine(_logPath, line);
            }

            QueueRemote(level, message);
        }
        catch
        {
            try
            {
                var fallbackLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}] {message}{Environment.NewLine}";
                lock (_syncRoot)
                {
                    WriteLine(_fallbackLogPath, fallbackLine);
                }
            }
            catch
            {
                // Logging should never stop the fallback sound agent.
            }
        }
    }

    private static void WriteLine(string path, string line)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(path, line);
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
            "AdminAudioAgent",
            NormalizeLevel(level),
            message,
            MachineName: Environment.MachineName,
            OrderId: ExtractOrderId(message),
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

    private static int? ExtractOrderId(string message)
    {
        var match = Regex.Match(message, @"OrderId=(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var orderId) ? orderId : null;
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
