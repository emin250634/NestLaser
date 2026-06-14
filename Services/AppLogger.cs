using System;
using System.IO;

namespace NestLaserDesktop.Services;

public static class AppLogger
{
    private static readonly object _lock = new();

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NestLaser",
        "logs");

    public static string ErrorLogPath => Path.Combine(LogDirectory, "error-log.txt");

    public static void LogError(Exception exception, string context = "")
    {
        if (exception == null) return;

        try
        {
            Directory.CreateDirectory(LogDirectory);
            lock (_lock)
            {
                File.AppendAllText(ErrorLogPath,
                    "------------------------------------------------------------" + Environment.NewLine +
                    $"DateTime       : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine +
                    $"Context        : {context}" + Environment.NewLine +
                    $"Exception Type : {exception.GetType().FullName}" + Environment.NewLine +
                    $"Message        : {exception.Message}" + Environment.NewLine +
                    $"Stack Trace    : {exception.StackTrace}" + Environment.NewLine +
                    Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }

    public static void LogMessage(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            lock (_lock)
            {
                File.AppendAllText(ErrorLogPath,
                    "------------------------------------------------------------" + Environment.NewLine +
                    $"DateTime : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine +
                    $"Message  : {message}" + Environment.NewLine +
                    Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
