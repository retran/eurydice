using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File changed event.
    /// </summary>
    public sealed class FileChangedEvent : FileSystemEvent
    {
        public FileChangedEvent(FileSystemEntryId id, long size)
            : base(id)
        {
            Size = size;
        }

        public long Size { get; }
    }
}