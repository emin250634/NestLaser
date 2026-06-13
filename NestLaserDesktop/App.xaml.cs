using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NestLaserDesktop.Services;

namespace NestLaserDesktop;

public partial class App : Application
{
    private string? _lastError;
    private int _errorCount;
    private const int MaxErrorsBeforeSilence = 3;

    private const string FirstRunFlagFile = "first-run.done";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        EnsureAppDataFolders();
    }

    private void EnsureAppDataFolders()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NestLaser");

        string flagPath = Path.Combine(appDataPath, FirstRunFlagFile);

        bool isFirstRun = !File.Exists(flagPath);

        string[] requiredFolders = new[]
        {
            Path.Combine(appDataPath, "profiles"),
            Path.Combine(appDataPath, "projects"),
            Path.Combine(appDataPath, "backups"),
            Path.Combine(appDataPath, "logs")
        };

        foreach (string folder in requiredFolders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        if (isFirstRun)
        {
            try
            {
                File.WriteAllText(flagPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AppLogger.LogMessage($"First run setup completed. AppData created at: {appDataPath}");
            }
            catch
            {
            }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        _errorCount++;
        AppLogger.LogError(e.Exception, "DispatcherUnhandledException");

        string currentError = $"{e.Exception.GetType().Name}: {e.Exception.Message}";

        if (_errorCount <= MaxErrorsBeforeSilence && currentError != _lastError)
        {
            _lastError = currentError;
            MessageBox.Show(
                $"Beklenmeyen hata oluştu:\n{e.Exception.Message}\n\nUygulama çalışmaya devam edecek.",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            AppLogger.LogError(ex, "AppDomain UnhandledException");
        else
            AppLogger.LogMessage($"AppDomain UnhandledException: {e.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLogger.LogError(e.Exception, "TaskScheduler UnobservedTaskException");
        e.SetObserved();
    }
}
