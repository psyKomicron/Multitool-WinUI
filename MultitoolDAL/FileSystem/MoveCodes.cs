namespace Multitool.Data
{
    /// <summary>
    /// Should be internal but <see langword="protected internal"/> method does not work.
    /// </summary>
    public enum MoveCodes
    {
        /// <summary>
        /// If the target path does not exist.
        /// </summary>
        PathNotFound,
        /// <summary>
        /// If the file is a system file/
        /// </summary>
        IsSystem,
        /// <summary>
        /// If the internal <see cref="System.IO.FileSystemInfo"/> is <see langword="null"/>.
        /// </summary>
        InfoNotSet,
        /// <summary>
        /// If the move is possible.
        /// </summary>
        Possible
    }
}
