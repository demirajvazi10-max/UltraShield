using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace UltraShield
{
    public partial class App : Application
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltraShield", "error.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

            // Global exception handlers, same pattern as Ultra Video Editor:
            // never let an unhandled exception silently crash without a trace.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);
            MessageBox.Show(
                "An unexpected error occurred and has been logged. UltraShield will try to continue running.",
                "UltraShield - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError(ex);
            }
        }

        private static void LogError(Exception ex)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:u} {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never itself throw and mask the original error.
            }
        }
    }
}
