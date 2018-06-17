using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File system event.
    /// </summary>
    public abstract class FileSystemEvent
    {
        protected FileSystemEvent(FileSystemEntryId id)
        {
            Id = id;
        }

        public FileSystemEntryId Id { get; }
    }
}