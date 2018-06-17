namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system state has changed.
    /// </summary>
    public class FileSystemModelStateChangedEvent : FileSystemModelEvent
    {
        public FileSystemModelStateChangedEvent(FileSystemModelState state)
        {
            State = state;
        }

        public FileSystemModelState State { get; }
    }
}