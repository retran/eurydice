using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     Directory created event.
    /// </summary>
    public sealed class DirectoryCreatedEvent : FileSystemEvent
    {
        public DirectoryCreatedEvent(string name, FileSystemEntryId id, FileSystemEntryId parentId)
            : base(id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ParentId = parentId;
        }

        public string Name { get; }
        public FileSystemEntryId ParentId { get; }
    }
}