using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Model
{
    /// <summary>
    ///     File system entry model.
    /// </summary>
    public abstract class FileSystemModelEntry
    {
        protected const double Epsilon = 0.0000001;

        protected readonly FileSystemModel Model;
        private double _lastNotifiedEnd;

        private long _lastNotifiedSize;
        private double _lastNotifiedStart;
        private bool _lastNotifiedVisible;

        protected FileSystemModelEntry(FileSystemModel fileSystemModel, FileSystemEntryId id, DirectoryEntry parent,
            string name,
            long size)
        {
            Model = fileSystemModel ?? throw new ArgumentNullException(nameof(fileSystemModel));
            Id = id;
            Parent = parent;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Size = size;
        }

        internal FileSystemEntryId Id { get; private set; }
        internal DirectoryEntry Parent { get; }
        internal string Name { get; private set; }
        internal long Size { get; set; }
        internal bool Visible { get; set; }
        internal double Start { get; set; }
        internal double End { get; set; } = 1;
        internal bool Reindexed { get; set; }

        internal void NotifyNodeChanged()
        {
            if (Size == _lastNotifiedSize
                && Math.Abs(_lastNotifiedStart - Start) < Epsilon
                && Math.Abs(_lastNotifiedEnd - End) < Epsilon)
                return;

            _lastNotifiedSize = Size;
            _lastNotifiedStart = Start;
            _lastNotifiedEnd = End;

            if (Visible) Model.RaiseNodeChanged(Id, Size, Start, End);
        }

        internal void Rename(string name, FileSystemEntryId newId)
        {
            if (Visible) Model.RaiseNodeRenamed(name, Id, newId);

            Name = name;
            Id = newId;
        }

        internal void NotifyVisibilityChanged()
        {
            if (Visible == _lastNotifiedVisible) return;

            _lastNotifiedVisible = Visible;
            if (_lastNotifiedVisible)
            {
                Model.RaiseNodeCreated(Id, Parent?.Id ?? FileSystemEntryId.Empty, Name);
            }
            else
            {
                _lastNotifiedStart = 0;
                _lastNotifiedEnd = 0;
                _lastNotifiedSize = 0;
                Model.RaiseNodeDeleted(Id);
            }
        }
    }
}