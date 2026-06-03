namespace CafeOrders.AdminAudioAgent;

public sealed class AgentLogger(string logPath)
{
    private readonly object _syncRoot = new();
    private readonly string _logPath = string.IsNullOrWhiteSpace(logPath)
        ? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CafeOrders",
            "AdminAudioAgent",
            "AdminAudioAgent.log")
        : logPath;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} {exception.Message}");

    private void Write(string level, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}] {message}{Environment.NewLine}";
            lock (_syncRoot)
            {
                File.AppendAllText(_logPath, line);
            }
        }
        catch
        {
            // Logging should never stop the fallback sound agent.
        }
    }
}
