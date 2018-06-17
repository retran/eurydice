using Eurydice.Core.Common;

namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system entry node has been deleted.
    /// </summary>
    public sealed class NodeDeletedEvent : FileSystemModelEvent
    {
        public NodeDeletedEvent(FileSystemEntryId id)
        {
            Id = id;
        }

        public FileSystemEntryId Id { get; }
    }
}