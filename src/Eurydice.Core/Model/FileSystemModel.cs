using System;
using System.Collections.Generic;
using System.Linq;
using Eurydice.Core.Common;
using Eurydice.Core.Indexer;

namespace Eurydice.Core.Model
{
    /// <summary>
    ///     File system model.
    /// </summary>
    public sealed class FileSystemModel
    {
        public delegate void NodeChangedEventHandler(FileSystemEntryId id, long size, double start, double end);

        public delegate void NodeCreatedEventHandler(FileSystemEntryId id, FileSystemEntryId parentId, string name);

        public delegate void NodeDeletedEventHandler(FileSystemEntryId id);

        public delegate void NodeRenamedEventHandler(string name, FileSystemEntryId oldId, FileSystemEntryId newId);

        private readonly Dictionary<FileSystemEntryId, DirectoryEntry> _directoryLookupTable
            = new Dictionary<FileSystemEntryId, DirectoryEntry>();

        private readonly Dictionary<FileSystemEntryId, FileEntry> _fileLookupTable
            = new Dictionary<FileSystemEntryId, FileEntry>();

        private readonly DirectoryEntry _root;

        private readonly FileSystemEntryId _rootId;

        internal readonly double EntrySizeThreshold;

        internal readonly int MaxVisibleEntries;

        public FileSystemModel(string rootNodeName, FileSystemEntryId rootId, int maxVisibleEntries = 10, double entrySizeTreshold = 0.1)
        {
            MaxVisibleEntries = maxVisibleEntries;
            EntrySizeThreshold = entrySizeTreshold;

            _rootId = rootId;
            _root = new DirectoryEntry(this, rootId, null, rootNodeName);
            _directoryLookupTable.Add(_rootId, _root);
        }

        public event NodeCreatedEventHandler NodeCreated;
        public event NodeChangedEventHandler NodeChanged;
        public event NodeRenamedEventHandler NodeRenamed;
        public event NodeDeletedEventHandler NodeDeleted;

        /// <summary>
        ///     Initialize model.
        /// </summary>
        public void Initialize()
        {
            _root.Visible = true;
            _root.NotifyVisibilityChanged();
            _root.NotifyNodeChanged();
        }

        /// <summary>
        ///     Populate model with already indexed entries.
        /// </summary>
        /// <param name="parentEntry"></param>
        /// <param name="entries"></param>
        public void PopulateWithIndexedEntries(DirectoryEntry parentEntry, IEnumerable<FileSystemModelEntry> entries)
        {
            var ordered = entries.OrderByDescending(e => e.Size).ToList();

            foreach (var entry in ordered)
                switch (entry)
                {
                    case DirectoryEntry directoryEntry:
                        _directoryLookupTable.Add(directoryEntry.Id, directoryEntry);
                        break;
                    case FileEntry fileEntry:
                        _fileLookupTable.Add(fileEntry.Id, fileEntry);
                        break;
                }

            parentEntry.PrepopulateWithSortedEntries(ordered);
        }

        /// <summary>
        ///     Update model with reindexed entries.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="entries"></param>
        public void UpdateWithIndexedEntries(DirectoryEntry parent, IEnumerable<FileSystemEntryInfo> entries)
        {
            foreach (var entry in entries)
                switch (entry)
                {
                    case DirectoryEntryInfo directoryEntryInfo:
                        if (!_directoryLookupTable.TryGetValue(directoryEntryInfo.Id, out var directoryEntry))
                            directoryEntry = DoCreateDirectory(directoryEntryInfo.Id, parent, directoryEntryInfo.Name);

                        directoryEntry.Reindexed = true;
                        break;
                    case FileEntryInfo fileEntryInfo:
                        if (!_fileLookupTable.TryGetValue(fileEntryInfo.Id, out var fileEntry))
                            fileEntry = DoCreateFile(fileEntryInfo.Id, parent, fileEntryInfo.Name, fileEntryInfo.Size);
                        else if (fileEntry.Size != fileEntryInfo.Size) fileEntry.ChangeSize(fileEntryInfo.Size);

                        fileEntry.Reindexed = true;
                        break;
                }

            foreach (var entry in parent.EnumerateAll().ToList())
            {
                if (entry.Reindexed)
                {
                    entry.Reindexed = false;
                    continue;
                }

                switch (entry)
                {
                    case DirectoryEntry directoryEntry:
                        DoDeleteDirectory(directoryEntry);
                        break;
                    case FileEntry fileEntry:
                        DoDeleteFile(fileEntry);
                        break;
                }
            }
        }

        /// <summary>
        ///     Returns directory model by id.
        /// </summary>
        public DirectoryEntry LookupDirectory(FileSystemEntryId id)
        {
            return _directoryLookupTable[id];
        }

        /// <summary>
        ///     Returns file model by id.
        /// </summary>
        public FileEntry LookupFile(FileSystemEntryId id)
        {
            return _fileLookupTable[id];
        }

        /// <summary>
        ///     Create file entry model.
        /// </summary>
        public void CreateFile(FileSystemEntryId id, FileSystemEntryId parentId, string name, long size)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!_directoryLookupTable.TryGetValue(parentId, out var parentEntry)) return;

            if (_fileLookupTable.TryGetValue(id, out _)) return;

            DoCreateFile(id, parentEntry, name, size);
        }

        /// <summary>
        ///     Create directory entry model.
        /// </summary>
        public void CreateDirectory(FileSystemEntryId id, FileSystemEntryId parentId, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (_directoryLookupTable.ContainsKey(id)) return;

            if (!_directoryLookupTable.TryGetValue(parentId, out var parentEntry)) return;

            DoCreateDirectory(id, parentEntry, name);
        }

        /// <summary>
        ///     Change file entry size.
        /// </summary>
        public void ChangeFileSize(FileSystemEntryId id, long size)
        {
            if (!_fileLookupTable.TryGetValue(id, out var fileEntry)) return;

            if (fileEntry.Size == size) return;

            fileEntry.ChangeSize(size);
        }

        /// <summary>
        ///     Rename file entry.
        /// </summary>
        public void RenameFile(string newName, FileSystemEntryId oldId, FileSystemEntryId newId)
        {
            if (newName == null) throw new ArgumentNullException(nameof(newName));

            if (!_fileLookupTable.TryGetValue(oldId, out var fileEntry)) return;

            _fileLookupTable.Remove(fileEntry.Id);
            _fileLookupTable.Add(newId, fileEntry);

            fileEntry.Rename(newName, newId);
        }

        /// <summary>
        ///     Rename directory entry.
        /// </summary>
        public void RenameDirectory(string newName, FileSystemEntryId oldId, FileSystemEntryId newId)
        {
            if (newName == null) throw new ArgumentNullException(nameof(newName));

            if (!_directoryLookupTable.TryGetValue(oldId, out var directoryEntry)) return;

            _directoryLookupTable.Remove(directoryEntry.Id);
            _directoryLookupTable.Add(newId, directoryEntry);

            directoryEntry.Rename(newName, newId);
        }

        /// <summary>
        ///     Delete file system model entry.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteEntry(FileSystemEntryId id)
        {
            if (_fileLookupTable.TryGetValue(id, out var fileEntry))
                DoDeleteFile(fileEntry);
            else if (_directoryLookupTable.TryGetValue(id, out var directoryEntry))
                DoDeleteDirectory(directoryEntry);
        }

        /// <summary>
        ///     Update file system model tree and make it consistent.
        /// </summary>
        public void Update()
        {
            var queue = new Queue<DirectoryEntry>();
            queue.Enqueue(_root);

            _root.NotifyNodeChanged();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                current.Update();

                foreach (var entry in current.EnumerateAll())
                    if (entry is DirectoryEntry directoryEntry)
                        queue.Enqueue(directoryEntry);
            }
        }

        internal void RaiseNodeCreated(FileSystemEntryId id, FileSystemEntryId parentId, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            NodeCreated?.Invoke(id, parentId, name);
        }

        internal void RaiseNodeChanged(FileSystemEntryId id, long size, double start, double end)
        {
            NodeChanged?.Invoke(id, size, start, end);
        }

        internal void RaiseNodeRenamed(string name, FileSystemEntryId oldId, FileSystemEntryId newId)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            NodeRenamed?.Invoke(name, oldId, newId);
        }

        internal void RaiseNodeDeleted(FileSystemEntryId id)
        {
            NodeDeleted?.Invoke(id);
        }

        private FileEntry DoCreateFile(FileSystemEntryId id, DirectoryEntry parentEntry, string name, long size)
        {
            var fileEntry = new FileEntry(this, id, parentEntry, name, size);
            _fileLookupTable.Add(id, fileEntry);
            parentEntry.Add(fileEntry);

            return fileEntry;
        }

        private DirectoryEntry DoCreateDirectory(FileSystemEntryId id, DirectoryEntry parentEntry, string name)
        {
            var directoryEntry = new DirectoryEntry(this, id, parentEntry, name);
            _directoryLookupTable.Add(id, directoryEntry);
            parentEntry.Add(directoryEntry);

            return directoryEntry;
        }

        private void DoDeleteDirectory(DirectoryEntry directoryEntry)
        {
            var queue = new Queue<DirectoryEntry>();
            queue.Enqueue(directoryEntry);

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                _directoryLookupTable.Remove(current.Id);

                foreach (var entry in current.EnumerateAll())
                    switch (entry)
                    {
                        case FileEntry fileEntry:
                            _fileLookupTable.Remove(fileEntry.Id);
                            break;
                        case DirectoryEntry childDirectoryEntry:
                            queue.Enqueue(childDirectoryEntry);
                            break;
                    }
            }

            directoryEntry.Parent.Delete(directoryEntry);
        }

        private void DoDeleteFile(FileEntry fileEntry)
        {
            _fileLookupTable.Remove(fileEntry.Id);
            fileEntry.Parent.Delete(fileEntry);
        }
    }
}