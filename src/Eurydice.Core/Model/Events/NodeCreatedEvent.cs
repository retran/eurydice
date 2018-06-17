using Eurydice.Core.Common;

namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system model node has been created.
    /// </summary>
    public sealed class NodeCreatedEvent : FileSystemModelEvent
    {
        public NodeCreatedEvent(FileSystemEntryId id, FileSystemEntryId parentId, string name)
        {
            Name = name;
            ParentId = parentId;
            Id = id;
        }

        public string Name { get; }
        public FileSystemEntryId ParentId { get; }
        public FileSystemEntryId Id { get; }
    }
}