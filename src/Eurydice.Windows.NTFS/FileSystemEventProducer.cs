using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Eurydice.Core.Common;
using Eurydice.Core.Pipeline;
using Eurydice.Core.Watcher.Events;

namespace Eurydice.Core.Watcher
{
    /// <summary>
    ///     File system event producer implementation.
    /// </summary>
    public sealed class FileSystemEventProducer : IPipelineProducer<FileSystemEvent>, IDisposable
    {
        private readonly IFileSizeEstimator _fileSizeEstimator;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly BlockingCollection<Action> _fswEventBuffer = new BlockingCollection<Action>();
        private readonly string _path;
        private readonly FileSystemEntryId _rootId;

        private bool _disposed;

        private BlockingCollection<FileSystemEvent> _outputEventBuffer;

        public FileSystemEventProducer(IFileSizeEstimator fileSizeEstimator, string path,
            int internalBufferSize = 32768)
        {
            _fileSizeEstimator = fileSizeEstimator ?? throw new ArgumentNullException(nameof(fileSizeEstimator));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _rootId = new FileSystemEntryId(path);

            _fileSystemWatcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size,
                SynchronizingObject = new SynchronizingObject(_fswEventBuffer),
                InternalBufferSize = internalBufferSize
            };

            _fileSystemWatcher.Created += OnCreated;
            _fileSystemWatcher.Changed += OnChanged;
            _fileSystemWatcher.Renamed += OnRenamed;
            _fileSystemWatcher.Deleted += OnDeleted;
            _fileSystemWatcher.Error += OnError;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _fileSystemWatcher.EnableRaisingEvents = false;

            _fileSystemWatcher.Created -= OnCreated;
            _fileSystemWatcher.Changed -= OnChanged;
            _fileSystemWatcher.Renamed -= OnRenamed;
            _fileSystemWatcher.Deleted -= OnDeleted;
            _fileSystemWatcher.Error -= OnError;

            _fileSystemWatcher.Dispose();
        }

        public void Produce(BlockingCollection<FileSystemEvent> outputEventBuffer,
            CancellationToken cancellationToken)
        {
            if (outputEventBuffer == null) throw new ArgumentNullException(nameof(outputEventBuffer));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (_outputEventBuffer != null) throw new InvalidOperationException();

            Trace.TraceInformation("{0} executed.", nameof(FileSystemEventProducer));

            try
            {
                _outputEventBuffer = outputEventBuffer;

                _fileSystemWatcher.EnableRaisingEvents = true;

                foreach (var action in _fswEventBuffer.GetConsumingEnumerable(cancellationToken)) action.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Raise(new FileSystemUnrecoverableErrorEvent(_rootId, exception));
            }
            finally
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _outputEventBuffer = null;
                Trace.TraceInformation("{0} stopped.", nameof(FileSystemEventProducer));
            }
        }

        private void Raise(FileSystemEvent fileSystemEvent)
        {
            _outputEventBuffer.Add(fileSystemEvent);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("{0} {1}", e.ChangeType, e.FullPath);

            if (Directory.Exists(e.FullPath))
                Raise(new DirectoryCreatedEvent(Path.GetFileName(e.FullPath), new FileSystemEntryId(e.FullPath),
                    new FileSystemEntryId(Path.GetDirectoryName(e.FullPath))));
            else if (File.Exists(e.FullPath))
                try
                {
                    var size = _fileSizeEstimator.Estimate(e.FullPath);
                    Raise(new FileCreatedEvent(Path.GetFileName(e.FullPath), new FileSystemEntryId(e.FullPath),
                        new FileSystemEntryId(Path.GetDirectoryName(e.FullPath)), size));
                }
                catch (DirectoryNotFoundException)
                {
                    // do nothing
                }
                catch (FileNotFoundException)
                {
                    // do nothing
                }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("{0} {1}", e.ChangeType, e.FullPath);

            if (!File.Exists(e.FullPath)) return;

            try
            {
                var size = _fileSizeEstimator.Estimate(e.FullPath);
                Raise(new FileChangedEvent(new FileSystemEntryId(e.FullPath), size));
            }
            catch (DirectoryNotFoundException)
            {
                // do nothing
            }
            catch (FileNotFoundException)
            {
                // do nothing
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Trace.TraceInformation("{0} {1}", e.ChangeType, e.FullPath);

            if (Directory.Exists(e.FullPath))
                Raise(new DirectoryRenamedEvent(Path.GetFileName(e.FullPath), new FileSystemEntryId(e.FullPath),
                    new FileSystemEntryId(e.OldFullPath)));
            else if (File.Exists(e.FullPath))
                Raise(new FileRenamedEvent(Path.GetFileName(e.FullPath), new FileSystemEntryId(e.FullPath),
                    new FileSystemEntryId(e.OldFullPath)));
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("{0} {1}", e.ChangeType, e.FullPath);

            Raise(new FileSystemEntryDeletedEvent(new FileSystemEntryId(e.FullPath)));
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Trace.TraceError(e.GetException().Message);

            switch (e.GetException())
            {
                case InternalBufferOverflowException _:
                    Raise(new BufferOverflowEvent(_rootId));
                    _fileSystemWatcher.EnableRaisingEvents = true;
                    break;

                case Exception exception:
                    throw exception;
            }
        }

        private class SynchronizingObject : ISynchronizeInvoke
        {
            private readonly BlockingCollection<Action> _buffer;

            public SynchronizingObject(BlockingCollection<Action> buffer)
            {
                _buffer = buffer;
            }

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                _buffer.Add(() => method.DynamicInvoke(args));

                return null;
            }

            public object EndInvoke(IAsyncResult result)
            {
                // do nothing
                return null;
            }

            public object Invoke(Delegate method, object[] args)
            {
                throw new InvalidOperationException();
            }

            public bool InvokeRequired => false;
        }
    }
}