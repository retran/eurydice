using System;
using Eurydice.Core.Common;

namespace Eurydice.Core.Watcher.Events
{
    /// <summary>
    ///     File system event producer unrecoverable error event.
    /// </summary>
    public sealed class FileSystemUnrecoverableErrorEvent : FileSystemEvent
    {
        public FileSystemUnrecoverableErrorEvent(FileSystemEntryId id, Exception exception)
            : base(id)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public Exception Exception { get; }
    }
}