using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File system entry deleted event.
    /// </summary>
    public sealed class FileSystemEntryDeletedEvent : FileSystemEvent
    {
        public FileSystemEntryDeletedEvent(FileSystemEntryId id)
            : base(id)
        {
        }
    }
}