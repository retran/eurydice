using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using Eurydice.Windows.App.ViewModel;

namespace Eurydice.Windows.App.Commands
{
    /// <summary>
    ///     "Open folder" command.
    /// </summary>
    internal sealed class OpenFolderCommand : ICommand, IDisposable
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        private bool _canExecute = true;
        private bool _disposed;

        public OpenFolderCommand(MainWindowViewModel mainWindowViewModel)
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
            using (var folderBroserDialog = new FolderBrowserDialog())
            {
                var result = folderBroserDialog.ShowDialog();
                if (result == DialogResult.OK)
                    _mainWindowViewModel.Start(folderBroserDialog.SelectedPath);
            }
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
                _canExecute = !_mainWindowViewModel.Running;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}