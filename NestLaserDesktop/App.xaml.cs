using System;
using System.Windows;
using System.Windows.Threading;

namespace NestLaserDesktop;

public partial class App : Application
{
    private string? _lastError;
    private int _errorCount;
    private const int MaxErrorsBeforeSilence = 3;

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        _errorCount++;

        string currentError = $"{e.Exception.GetType().Name}: {e.Exception.Message}";

        if (_errorCount <= MaxErrorsBeforeSilence && currentError != _lastError)
        {
            _lastError = currentError;
            MessageBox.Show(
                $"Beklenmeyen hata oluştu:\n{e.Exception.Message}\n\nUygulama çalışmaya devam edecek.",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

