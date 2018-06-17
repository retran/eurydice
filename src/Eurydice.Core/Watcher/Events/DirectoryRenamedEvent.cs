using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     Directory renamed event.
    /// </summary>
    public sealed class DirectoryRenamedEvent : FileSystemEvent
    {
        public DirectoryRenamedEvent(string name, FileSystemEntryId id, FileSystemEntryId oldId)
            : base(id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OldId = oldId;
        }

        public string Name { get; }
        public FileSystemEntryId OldId { get; }
    }
}