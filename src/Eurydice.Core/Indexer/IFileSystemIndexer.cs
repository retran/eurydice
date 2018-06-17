using System.Collections.Generic;

namespace Eurydice.Core.Indexer
{
    /// <summary>
    ///     File system indexer
    /// </summary>
    public interface IFileSystemIndexer
    {
        /// <summary>
        ///     Enumerate file entries in a path.
        /// </summary>
        IEnumerable<FileSystemEntryInfo> EnumerateFileSystemEntries(string path);

        /// <summary>
        ///     Throw exception if directory not exists.
        /// </summary>
        void ThrowIfDirectoryNotExists(string path);
    }
}