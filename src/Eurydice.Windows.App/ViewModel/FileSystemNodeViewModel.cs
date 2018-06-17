using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using Eurydice.Core.Common;
using Eurydice.Windows.App.Common;
using Eurydice.Windows.App.Controls.ViewModel;

namespace Eurydice.Windows.App.ViewModel
{
    /// <summary>
    ///     File system node view model.
    /// </summary>
    internal sealed class FileSystemNodeViewModel : ViewModelBase, ISunburstChartNodeViewModel<FileSystemNodeViewModel>
    {
        private Brush _brush;
        private double _end;
        private Brush _hoverBrush;
        private FileSystemEntryId _id;
        private string _name;
        private ObservableCollection<FileSystemNodeViewModel> _nodes;
        private double _relativeEnd;
        private double _relativeStart;
        private long _size;
        private double _start;

        public FileSystemNodeViewModel(ICommand drilldownCommand, FileSystemEntryId id, FileSystemNodeViewModel parent,
            string name)
        {
            _id = id;
            Parent = parent;
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _size = 0;
            _brush = Brushes.LightGray;
            _hoverBrush = new SolidColorBrush(Color.FromArgb(255, 20, 10, 80));

            _nodes = new ObservableCollection<FileSystemNodeViewModel>();

            DrilldownCommand = drilldownCommand ?? throw new ArgumentNullException(nameof(drilldownCommand));
        }

        public FileSystemEntryId Id
        {
            get => _id;
            private set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public FileSystemNodeViewModel Parent { get; }

        public string Name
        {
            get => _name;
            private set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public long Size
        {
            get => _size;
            private set
            {
                _size = value;
                OnPropertyChanged(nameof(Size));
            }
        }

        public bool IsRoot { get; set; }

        public ICommand DrilldownCommand { get; }

        public double Start
        {
            get => _relativeStart;
            private set
            {
                _relativeStart = value;
                OnPropertyChanged(nameof(Start));
            }
        }

        public double End
        {
            get => _relativeEnd;
            private set
            {
                _relativeEnd = value;
                OnPropertyChanged(nameof(End));
            }
        }

        public ObservableCollection<FileSystemNodeViewModel> Nodes
        {
            get => _nodes;
            private set
            {
                _nodes = value;
                OnPropertyChanged(nameof(Nodes));
            }
        }

        public Brush Brush
        {
            get => _brush;
            set
            {
                _brush = value;
                OnPropertyChanged(nameof(Brush));
            }
        }

        public Brush HoverBrush
        {
            get => _hoverBrush;
            set
            {
                _hoverBrush = value;
                OnPropertyChanged(nameof(HoverBrush));
            }
        }

        public void Change(long size, double start, double end)
        {
            Size = size;
            _start = start;
            _end = end;
        }

        public void Update()
        {
            if (Parent == null || IsRoot)
            {
                Start = 0;
                End = 1;
            }
            else
            {
                var parentLength = Parent.End - Parent.Start;
                Start = Parent.Start + _start * parentLength;
                End = Parent.Start + _end * parentLength;
            }

            UpdateBrush();
        }

        private void UpdateBrush()
        {
            if (Id.IsHiddenEntriesNode)
            {
                _brush = Brushes.LightGray;
                return;
            }

            var length = End - Start;

            var r = (byte) (190 - 150 * length);
            var g = (byte) (180 - 150 * length);
            var b = (byte) (250 - 150 * length);

            Brush = new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }

        public void Rename(FileSystemEntryId newId, string name)
        {
            Id = newId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}