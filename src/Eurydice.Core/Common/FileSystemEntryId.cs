namespace Eurydice.Core.Common
{
    /// <summary>
    ///     File system entry identifier.
    /// </summary>
    public struct FileSystemEntryId
    {
        /// <summary>
        ///     Empty identifier.
        /// </summary>
        public static readonly FileSystemEntryId Empty = new FileSystemEntryId(string.Empty);

        /// <summary>
        ///     File system entry path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     True if identifier was created for "hidden entries" node.
        /// </summary>
        public bool IsHiddenEntriesNode { get; }

        public FileSystemEntryId(string path, bool isHiddenEntriesNode = false)
        {
            IsHiddenEntriesNode = isHiddenEntriesNode;
            Path = isHiddenEntriesNode ? path + @"\%%hidden%%" : path;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var other = (FileSystemEntryId) obj;
            return Path.Equals(other.Path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}