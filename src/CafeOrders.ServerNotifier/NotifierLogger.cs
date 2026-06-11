using System.IO;

namespace CafeOrders.ServerNotifier;

public sealed class NotifierLogger(string logPath)
{
    private readonly object _syncRoot = new();
    private const long MaxLogSizeBytes = 1_000_000;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} {exception.GetType().Name}: {exception.Message}");

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
}
