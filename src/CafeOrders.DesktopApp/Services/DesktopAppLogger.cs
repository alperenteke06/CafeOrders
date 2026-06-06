using System;
using System.IO;

namespace CafeOrders.DesktopApp.Services;

public static class DesktopAppLogger
{
    private const long MaxLogSizeBytes = 5 * 1024 * 1024;
    private static readonly object SyncRoot = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "DesktopApp.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}");

    public static string CurrentLogPath => LogPath;

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
}
