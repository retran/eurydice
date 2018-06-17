using Eurydice.Core.Common;

namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system model node has been renamed.
    /// </summary>
    public sealed class NodeRenamedEvent : FileSystemModelEvent
    {
        public NodeRenamedEvent(string name, FileSystemEntryId oldId, FileSystemEntryId newId)
        {
            Name = name;
            OldId = oldId;
            NewId = newId;
        }

        public string Name { get; }
        public FileSystemEntryId OldId { get; }
        public FileSystemEntryId NewId { get; }
    }
}