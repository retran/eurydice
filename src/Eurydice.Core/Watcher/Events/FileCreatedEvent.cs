using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File created event.
    /// </summary>
    public sealed class FileCreatedEvent : FileSystemEvent
    {
        public FileCreatedEvent(string name, FileSystemEntryId id, FileSystemEntryId parentId, long size)
            : base(id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ParentId = parentId;
            Size = size;
        }

        public string Name { get; }
        public FileSystemEntryId ParentId { get; }
        public long Size { get; }
    }
}