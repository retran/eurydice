using System;

namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system model has encountered an error.
    /// </summary>
    public sealed class FileSystemModelUnrecoverableErrorEvent : FileSystemModelEvent
    {
        public FileSystemModelUnrecoverableErrorEvent(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}