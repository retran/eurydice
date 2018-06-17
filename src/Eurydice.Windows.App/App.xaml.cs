using System;
using System.Windows;

namespace Eurydice.Windows.App
{
    public partial class App : Application
    {
        public App()
        {
            IDisposable mainWindowViewModel = null;

            Startup += (sender, args) =>
            {
                MainWindow = new MainWindow();
                mainWindowViewModel = MainWindow.DataContext as IDisposable;
                MainWindow.Show();
            };

            DispatcherUnhandledException += (sender, args) => mainWindowViewModel?.Dispose();

            Exit += (sender, args) => mainWindowViewModel?.Dispose();
        }
    }
}