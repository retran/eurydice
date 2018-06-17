using Eurydice.Core.Common;

namespace Eurydice.Core.Model.Events
{
    /// <summary>
    ///     File system model node size has changed.
    /// </summary>
    public sealed class NodeChangedEvent : FileSystemModelEvent
    {
        public NodeChangedEvent(FileSystemEntryId id, long size, double start, double end)
        {
            Id = id;
            Size = size;
            Start = start;
            End = end;
        }

        public FileSystemEntryId Id { get; }
        public long Size { get; }
        public double Start { get; }
        public double End { get; }
    }
}