namespace CafeOrders.AdminAudioAgent;

public sealed class AgentLogger(string logPath, string? fallbackDirectory = null)
{
    private readonly object _syncRoot = new();
    private readonly string _logPath = string.IsNullOrWhiteSpace(logPath)
        ? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CafeOrders",
            "AdminAudioAgent",
            "AdminAudioAgent.log")
        : logPath;
    private readonly string _fallbackLogPath = Path.Combine(
        string.IsNullOrWhiteSpace(fallbackDirectory) ? AppContext.BaseDirectory : fallbackDirectory,
        "AdminAudioAgent.log");

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} {exception.Message}");

    private void Write(string level, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}] {message}{Environment.NewLine}";
            lock (_syncRoot)
            {
                WriteLine(_logPath, line);
            }
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
}
