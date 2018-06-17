using Eurydice.Core.Common;

namespace Eurydice.Core.Model
{
    /// <summary>
    ///     File entry model.
    /// </summary>
    public sealed class FileEntry : FileSystemModelEntry
    {
        public FileEntry(FileSystemModel fileSystemModel, FileSystemEntryId id, DirectoryEntry parent, string name,
            long size)
            : base(fileSystemModel, id, parent, name, size)
        {
        }

        internal void ChangeSize(long size)
        {
            if (Size == size) return;

            Parent.UpdateSize(size - Size);
            Size = size;
            Parent.UpdateEntryPosition(this);
            Parent.UpdateEntriesVisibility();
        }
    }
}