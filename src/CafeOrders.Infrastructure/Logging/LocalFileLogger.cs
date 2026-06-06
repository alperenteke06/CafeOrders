using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CafeOrders.Infrastructure.Logging;

public static class LocalFileLoggerExtensions
{
    public static ILoggingBuilder AddLocalFile(this ILoggingBuilder logging, IConfiguration configuration, string defaultFileName)
    {
        var configuredPath = configuration["Logging:FilePath"];
        var logPath = ResolveLogPath(configuredPath, defaultFileName);
        logging.AddProvider(new LocalFileLoggerProvider(logPath));
        return logging;
    }

    private static string ResolveLogPath(string? configuredPath, string defaultFileName)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? defaultFileName : configuredPath.Trim();
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}

public sealed class LocalFileLoggerProvider(string logPath) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, LocalFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new LocalFileLogger(name, logPath, _syncRoot));

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public sealed class LocalFileLogger(string categoryName, string logPath, object syncRoot) : ILogger
{
    private const long MaxLogSizeBytes = 10 * 1024 * 1024;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{logLevel}][{categoryName}] {message}";
            if (exception is not null)
            {
                line += $" | {exception.GetType().Name}: {exception.Message}";
            }

            lock (syncRoot)
            {
                var directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RotateIfNeeded();
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never interrupt API/WebUI request processing.
        }
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

        var archivePath = Path.Combine(fileInfo.DirectoryName!, $"{Path.GetFileNameWithoutExtension(logPath)}.previous{fileInfo.Extension}");
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(logPath, archivePath);
    }
}
