using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Eurydice.Core.Common;
using Eurydice.Core.Indexer;
using Eurydice.Core.Model.Events;
using Eurydice.Core.Pipeline;
using Eurydice.Core.Watcher.Events;

namespace Eurydice.Core.Model
{
    /// <summary>
    ///     File system model processing stage.
    /// </summary>
    public sealed class FileSystemModelStage : IPipelineStage<FileSystemEvent, FileSystemModelEvent>, IDisposable
    {
        private readonly IFileSystemIndexer _fileSystemIndexer;
        private readonly FileSystemModel _fileSystemModel;
        private readonly FileSystemEntryId _rootId;
        private readonly string _rootPath;
        private readonly TimeSpan _updateInterval;

        private bool _disposed;
        private BlockingCollection<FileSystemModelEvent> _outputEventBuffer;

        public FileSystemModelStage(IFileSystemIndexer fileSystemIndexer, string rootNodeName, string rootPath,
            TimeSpan updateInterval)
        {
            if (rootNodeName == null) throw new ArgumentNullException(nameof(rootNodeName));

            _updateInterval = updateInterval;
            _fileSystemIndexer = fileSystemIndexer ?? throw new ArgumentNullException(nameof(fileSystemIndexer));
            _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            _rootId = new FileSystemEntryId(rootPath);
            _fileSystemModel = new FileSystemModel(rootNodeName, _rootId);

            _fileSystemModel.NodeCreated += OnNodeCreated;
            _fileSystemModel.NodeChanged += OnNodeChanged;
            _fileSystemModel.NodeRenamed += OnNodeRenamed;
            _fileSystemModel.NodeDeleted += OnNodeDeleted;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _fileSystemModel.NodeCreated -= OnNodeCreated;
            _fileSystemModel.NodeChanged -= OnNodeChanged;
            _fileSystemModel.NodeRenamed -= OnNodeRenamed;
            _fileSystemModel.NodeDeleted -= OnNodeDeleted;
        }

        public void Execute(BlockingCollection<FileSystemEvent> inputEventBuffer,
            BlockingCollection<FileSystemModelEvent> outputEventBuffer,
            CancellationToken cancellationToken)
        {
            if (inputEventBuffer == null) throw new ArgumentNullException(nameof(inputEventBuffer));
            if (outputEventBuffer == null) throw new ArgumentNullException(nameof(outputEventBuffer));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (_outputEventBuffer != null) throw new InvalidOperationException();

            Trace.TraceInformation("{0} executed.", nameof(FileSystemModelStage));

            try
            {
                _outputEventBuffer = outputEventBuffer;

                _fileSystemModel.Initialize();

                Index(cancellationToken);

                DispatchEvents(inputEventBuffer, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message);
                Raise(new FileSystemModelUnrecoverableErrorEvent(exception));
            }
            finally
            {
                Raise(new FileSystemModelStateChangedEvent(FileSystemModelState.Stopped));
                _outputEventBuffer = null;
                Trace.TraceInformation("{0} stopped.", nameof(FileSystemModelStage));
            }
        }

        private void Index(CancellationToken cancellationToken)
        {
            Raise(new FileSystemModelStateChangedEvent(FileSystemModelState.Indexing));
            DoIndex(_rootId, (parent, entries) =>
            {
                _fileSystemModel.PopulateWithIndexedEntries(parent,
                    entries.Select<FileSystemEntryInfo, FileSystemModelEntry>(entry =>
                    {
                        switch (entry)
                        {
                            case FileEntryInfo fileEntryInfo:
                                return new FileEntry(_fileSystemModel, fileEntryInfo.Id, parent,
                                    fileEntryInfo.Name, fileEntryInfo.Size);

                            case DirectoryEntryInfo directoryEntryInfo:
                                return new DirectoryEntry(_fileSystemModel, directoryEntryInfo.Id, parent,
                                    directoryEntryInfo.Name);
                            default:
                                throw new InvalidOperationException();
                        }
                    }));
            }, cancellationToken);
        }

        private void DispatchEvents(BlockingCollection<FileSystemEvent> inputEventBuffer,
            CancellationToken cancellationToken)
        {
            Raise(new FileSystemModelStateChangedEvent(FileSystemModelState.Watching));
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dispatchStartedAt = DateTime.UtcNow;
                while (inputEventBuffer.TryTake(out var fileSystemEvent, (int) _updateInterval.TotalMilliseconds,
                    cancellationToken))
                {
                    Dispatch(fileSystemEvent, cancellationToken);

                    if (DateTime.UtcNow - dispatchStartedAt > _updateInterval) break;
                }

                _fileSystemIndexer.ThrowIfDirectoryNotExists(_rootPath);

                _fileSystemModel.Update();
            }
        }

        private void DoIndex(FileSystemEntryId id, Action<DirectoryEntry, IEnumerable<FileSystemEntryInfo>> add,
            CancellationToken cancellationToken)
        {
            var lastInvalidationDateTime = DateTime.Now;
            var currentParentId = id;
            var currentParent = _fileSystemModel.LookupDirectory(currentParentId);
            var entries = new List<FileSystemEntryInfo>();

            foreach (var fileSystemEntryInfo in _fileSystemIndexer.EnumerateFileSystemEntries(id.Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!currentParentId.Equals(fileSystemEntryInfo.ParentId))
                {
                    add(currentParent, entries);

                    entries.Clear();
                    currentParentId = fileSystemEntryInfo.ParentId;
                    currentParent = _fileSystemModel.LookupDirectory(currentParentId);

                    var now = DateTime.Now;
                    if (now - lastInvalidationDateTime > _updateInterval)
                    {
                        lastInvalidationDateTime = now;
                        _fileSystemModel.Update();
                    }
                }

                entries.Add(fileSystemEntryInfo);
            }

            add(currentParent, entries);
            _fileSystemModel.Update();
        }

        private void Reindex(FileSystemEntryId id, CancellationToken cancellationToken)
        {
            Raise(new FileSystemModelStateChangedEvent(FileSystemModelState.Reindexing));
            DoIndex(id, (parent, entries) => _fileSystemModel.UpdateWithIndexedEntries(parent, entries),
                cancellationToken);
            Raise(new FileSystemModelStateChangedEvent(FileSystemModelState.Watching));
        }

        private void Raise(FileSystemModelEvent fileSystemModelEvent)
        {
            _outputEventBuffer.Add(fileSystemModelEvent);
        }

        private void OnNodeCreated(FileSystemEntryId id, FileSystemEntryId parentId, string name)
        {
            Trace.TraceInformation("Node created: {0}", id);

            Raise(new NodeCreatedEvent(id, parentId, name));
        }

        private void OnNodeDeleted(FileSystemEntryId id)
        {
            Trace.TraceInformation("Node deleted: {0}", id);

            Raise(new NodeDeletedEvent(id));
        }

        private void OnNodeRenamed(string name, FileSystemEntryId oldId, FileSystemEntryId newId)
        {
            Trace.TraceInformation("Node renamed: {0}, {1}", oldId, newId);

            Raise(new NodeRenamedEvent(name, oldId, newId));
        }

        private void OnNodeChanged(FileSystemEntryId id, long size, double start, double end)
        {
            Trace.TraceInformation("Node changed: {0}, {1}, {2}, {3}", id, size, start, end);

            Raise(new NodeChangedEvent(id, size, start, end));
        }

        private void Dispatch(FileSystemEvent fileSystemEvent, CancellationToken cancellationToken)
        {
            switch (fileSystemEvent)
            {
                case FileCreatedEvent fileCreatedEvent:
                    _fileSystemModel.CreateFile(fileCreatedEvent.Id, fileCreatedEvent.ParentId,
                        fileCreatedEvent.Name, fileCreatedEvent.Size);
                    break;

                case DirectoryCreatedEvent directoryCreatedEvent:
                    _fileSystemModel.CreateDirectory(directoryCreatedEvent.Id, directoryCreatedEvent.ParentId,
                        directoryCreatedEvent.Name);
                    // can be moved directory, so let's reindex it
                    Reindex(directoryCreatedEvent.Id, cancellationToken);
                    break;

                case FileRenamedEvent fileRenamedEvent:
                    _fileSystemModel.RenameFile(fileRenamedEvent.Name, fileRenamedEvent.OldId,
                        fileRenamedEvent.Id);
                    break;

                case DirectoryRenamedEvent directoryRenamedEvent:
                    _fileSystemModel.RenameDirectory(directoryRenamedEvent.Name, directoryRenamedEvent.OldId,
                        directoryRenamedEvent.Id);
                    break;

                case FileChangedEvent fileChangedEvent:
                    _fileSystemModel.ChangeFileSize(fileChangedEvent.Id,
                        fileChangedEvent.Size);
                    break;

                case FileSystemEntryDeletedEvent entryDeletedEvent:
                    _fileSystemModel.DeleteEntry(entryDeletedEvent.Id);
                    break;

                case BufferOverflowEvent _:
                    Reindex(_rootId, cancellationToken);
                    break;

                case FileSystemUnrecoverableErrorEvent unrecoverableErrorEvent:
                    throw unrecoverableErrorEvent.Exception;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}