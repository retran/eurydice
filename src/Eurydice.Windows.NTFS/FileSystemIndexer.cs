using System.Collections.Generic;
using System.IO;
using Eurydice.Core;
using Eurydice.Core.Common;
using Eurydice.Core.Indexer;

namespace Eurydice.Windows.NTFS
{
    /// <summary>
    ///     File system indexer implementation.
    /// </summary>
    public class FileSystemIndexer : IFileSystemIndexer
    {
        private readonly IFileSizeEstimator _fileSizeEstimator;

        public FileSystemIndexer(IFileSizeEstimator fileSizeEstimator)
        {
            _fileSizeEstimator = fileSizeEstimator;
        }

        public IEnumerable<FileSystemEntryInfo> EnumerateFileSystemEntries(string path)
        {
            var queue = new Queue<DirectoryInfo>();
            var root = new DirectoryInfo(path);
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var fileSystemInfo in current.EnumerateFileSystemInfos())
                {
                    long size = 0;
                    var entryExists = true;
                    string parent = null;

                    switch (fileSystemInfo)
                    {
                        case FileInfo fileInfo:
                            try
                            {
                                size = _fileSizeEstimator.Estimate(fileInfo.FullName);
                                parent = fileInfo.Directory?.FullName;
                            }
                            catch (DirectoryNotFoundException)
                            {
                                entryExists = false;
                            }
                            catch (FileNotFoundException)
                            {
                                entryExists = false;
                            }

                            if (entryExists)
                                yield return new FileEntryInfo(fileInfo.Name,
                                    new FileSystemEntryId(fileInfo.FullName),
                                    new FileSystemEntryId(parent),
                                    size);

                            break;

                        case DirectoryInfo directoryInfo:
                            try
                            {
                                parent = directoryInfo.Parent?.FullName;
                            }
                            catch (DirectoryNotFoundException)
                            {
                                entryExists = false;
                            }

                            if (entryExists)
                                yield return new DirectoryEntryInfo(directoryInfo.Name,
                                    new FileSystemEntryId(directoryInfo.FullName),
                                    new FileSystemEntryId(parent));

                            queue.Enqueue(directoryInfo);
                            break;
                    }
                }
            }
        }

        public void ThrowIfDirectoryNotExists(string path)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
        }
    }
}