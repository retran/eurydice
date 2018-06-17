using Eurydice.Core.Common;

namespace Eurydice.Core.Indexer
{
    /// <summary>
    ///     Directory entry info.
    /// </summary>
    public sealed class DirectoryEntryInfo : FileSystemEntryInfo
    {
        public DirectoryEntryInfo(string name, FileSystemEntryId id, FileSystemEntryId parentId)
            : base(name, id, parentId)
        {
        }
    }
}