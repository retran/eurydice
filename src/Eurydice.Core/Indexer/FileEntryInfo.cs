using Eurydice.Core.Common;

namespace Eurydice.Core.Indexer
{
    /// <summary>
    ///     File entry info
    /// </summary>
    public sealed class FileEntryInfo : FileSystemEntryInfo
    {
        public FileEntryInfo(string name, FileSystemEntryId id, FileSystemEntryId parentId, long size)
            : base(name, id, parentId)
        {
            Size = size;
        }

        /// <summary>
        ///     File size estimation.
        /// </summary>
        public long Size { get; }
    }
}