using Eurydice.Core.Common;

namespace Eurydice.Core.Indexer
{
    /// <summary>
    ///     File system entry info.
    /// </summary>
    public abstract class FileSystemEntryInfo
    {
        protected FileSystemEntryInfo(string name, FileSystemEntryId id, FileSystemEntryId parentId)
        {
            Name = name;
            Id = id;
            ParentId = parentId;
        }

        /// <summary>
        ///     File system entry name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Unique identifier.
        /// </summary>
        public FileSystemEntryId Id { get; }

        /// <summary>
        ///     Unique identifier of parent entry.
        /// </summary>
        public FileSystemEntryId ParentId { get; }
    }
}