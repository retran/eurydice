namespace Eurydice.Core.Model
{
    /// <summary>
    ///     File system model processing state.
    /// </summary>
    public enum FileSystemModelState
    {
        Indexing,
        Watching,
        Reindexing,
        Stopped
    }
}