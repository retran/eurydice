using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File renamed event.
    /// </summary>
    public sealed class FileRenamedEvent : FileSystemEvent
    {
        public FileRenamedEvent(string name, FileSystemEntryId id, FileSystemEntryId oldId)
            : base(id)
        {
            OldId = oldId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public FileSystemEntryId OldId { get; }
        public string Name { get; }
    }
}