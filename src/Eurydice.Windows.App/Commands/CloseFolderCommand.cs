using System;
using System.ComponentModel;
using System.Windows.Input;
using Eurydice.Windows.App.ViewModel;

namespace Eurydice.Windows.App.Commands
{
    /// <summary>
    ///     "Close Folder" command.
    /// </summary>
    internal sealed class CloseFolderCommand : ICommand, IDisposable
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        private bool _canExecute;
        private bool _disposed;

        public CloseFolderCommand(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            _mainWindowViewModel.PropertyChanged += OnPropertyChanged;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            _mainWindowViewModel.Stop();
        }

        public event EventHandler CanExecuteChanged;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _mainWindowViewModel.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Running))
            {
                _canExecute = _mainWindowViewModel.Running;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}