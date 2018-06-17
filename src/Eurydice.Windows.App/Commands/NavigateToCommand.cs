using System;
using System.Windows.Input;
using Eurydice.Windows.App.ViewModel;

namespace Eurydice.Windows.App.Commands
{
    /// <summary>
    ///     "Navigate To" command.
    /// </summary>
    internal sealed class NavigateToCommand : ICommand
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        public NavigateToCommand(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            var node = (FileSystemNodeViewModel) parameter;
            _mainWindowViewModel.NavigateTo(node);
        }

        public event EventHandler CanExecuteChanged;
    }
}