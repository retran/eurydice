using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     Buffer overflow event.
    /// </summary>
    public sealed class BufferOverflowEvent : FileSystemEvent
    {
        public BufferOverflowEvent(FileSystemEntryId id)
            : base(id)
        {
        }
    }
}