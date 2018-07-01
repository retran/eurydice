using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Eurydice.Core.Common;
using Eurydice.Core.Model;
using Eurydice.Core.Watcher;
using Eurydice.Windows.App.Commands;
using Eurydice.Windows.App.Common;
using Eurydice.Windows.NTFS;

namespace Eurydice.Windows.App.ViewModel
{
    /// <summary>
    ///     Main window view model.
    /// </summary>
    internal sealed class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(100);

        private ObservableCollection<FileSystemNodeViewModel> _breadcrumbs;
        private CancellationTokenSource _cst;

        private ObservableCollection<FileSystemNodeViewModel> _current =
            new ObservableCollection<FileSystemNodeViewModel>();

        private bool _disposed;
        private FileSystemEventProducer _fileSystemEventProducer;
        private FileSystemModelEventConsumer _fileSystemModelEventConsumer;
        private FileSystemModelStage _fileSystemModelStage;

        private FileSystemViewModel _fileSystemViewModel;
        private Pipeline _pipeline;

        private bool _running;

        public MainWindowViewModel()
        {
            OpenFolderCommand = new OpenFolderCommand(this);
            NavigateToCommand = new NavigateToCommand(this);
            CloseFolderCommand = new CloseFolderCommand(this);
        }

        public OpenFolderCommand OpenFolderCommand { get; }
        public NavigateToCommand NavigateToCommand { get; }
        public CloseFolderCommand CloseFolderCommand { get; }

        public ObservableCollection<FileSystemNodeViewModel> Current
        {
            get => _current;
            set
            {
                _current = value;
                OnPropertyChanged(nameof(Current));
            }
        }

        public ObservableCollection<FileSystemNodeViewModel> Breadcrumbs
        {
            get => _breadcrumbs;
            set
            {
                _breadcrumbs = value;
                OnPropertyChanged(nameof(Breadcrumbs));
            }
        }

        public FileSystemViewModel FileSystemViewModel
        {
            get => _fileSystemViewModel;
            set
            {
                _fileSystemViewModel = value;
                OnPropertyChanged(nameof(FileSystemViewModel));
            }
        }

        public bool Running
        {
            get => _running;
            private set
            {
                _running = value;
                OnPropertyChanged(nameof(Running));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();

            _disposed = true;
            OpenFolderCommand.Dispose();
            CloseFolderCommand.Dispose();
        }

        public void Start(string path)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (_running) throw new InvalidOperationException();

            FileSystemViewModel = new FileSystemViewModel(NavigateToCommand);

            _fileSystemEventProducer = new FileSystemEventProducer(new FileSizeEstimator(), path);
            _fileSystemModelStage = new FileSystemModelStage(new FileSystemIndexer(new FileSizeEstimator()), path, path,
                _updateInterval);
            _fileSystemModelEventConsumer = new FileSystemModelEventConsumer(FileSystemViewModel);

            _pipeline = new Pipeline(_fileSystemEventProducer, _fileSystemModelStage, _fileSystemModelEventConsumer,
                () =>
                {
                    if (Current.Any()) _fileSystemViewModel.Update(Current.First());
                },
                _updateInterval);

            _pipeline.Run();

            Current = _fileSystemViewModel.Root;
            Breadcrumbs = _fileSystemViewModel.Root;

            _fileSystemViewModel.NodeDeleted += OnNodeDeleted;
            _fileSystemViewModel.ErrorReceived += OnErrorReceived;

            Running = true;
        }

        public void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (Running)
            {
                _pipeline?.Cancel();
                _fileSystemEventProducer?.Dispose();
                _fileSystemModelStage?.Dispose();
                _pipeline?.Dispose();
                _fileSystemViewModel.ErrorReceived -= OnErrorReceived;
                _fileSystemViewModel.NodeDeleted -= OnNodeDeleted;
                Running = false;
            }
        }

        public void NavigateTo(FileSystemNodeViewModel node)
        {
            Current.First().IsRoot = false;

            Current = new ObservableCollection<FileSystemNodeViewModel>
            {
                node
            };

            node.IsRoot = true;
            _fileSystemViewModel.Update(node);

            UpdateBreadcrumbs(node);
        }

        private void OnNodeDeleted(FileSystemEntryId id)
        {
            if (Breadcrumbs.Any(node => node.Id.Equals(id))) NavigateTo(Breadcrumbs.First());
        }

        private void OnErrorReceived(Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Stop();
        }

        private void UpdateBreadcrumbs(FileSystemNodeViewModel node)
        {
            Breadcrumbs.Clear();
            var current = node;

            var stack = new Stack<FileSystemNodeViewModel>();
            while (current != null)
            {
                stack.Push(current);
                current = current.Parent;
            }

            while (stack.Count > 0) Breadcrumbs.Add(stack.Pop());
        }
    }
}