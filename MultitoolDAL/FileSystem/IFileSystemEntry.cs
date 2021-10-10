using Multitool.DAL.FileSystem.Events;

using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.DAL
{
    /// <summary>
    /// Defines a <see cref="FileSystemInfo"/> decorator
    /// </summary>
    public interface IFileSystemEntry : IComparable, IComparable<IFileSystemEntry>, IEquatable<IFileSystemEntry>
    {
        #region properties

        /// <summary>
        /// Attributes
        /// </summary>
        FileAttributes Attributes { get; }

        /// <summary>
        /// The underlying <see cref="FileSystemInfo"/> decorated by <see cref="IFileSystemEntry"/>
        /// </summary>
        FileSystemInfo Info { get; }

        /// <summary>
        /// True if the file is compressed
        /// </summary>
        bool IsCompressed { get; }

        /// <summary>
        /// True is the file is considered device
        /// </summary>
        bool IsDevice { get; }

        /// <summary>
        /// True if the file is a directory
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// True if the file is encrypted
        /// </summary>
        bool IsEncrypted { get; }

        /// <summary>
        /// True if the file is hidden
        /// </summary>
        bool IsHidden { get; }

        /// <summary>
        /// True if the file is readonly
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// True if the file belongs to the system
        /// </summary>
        bool IsSystem { get; }

        /// <summary>
        /// Name of the file (not the path)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// True if the entry is marked as partial, meaning that this entry has not been fully computed yet.
        /// </summary>
        bool Partial { get; }

        /// <summary>
        /// Path of the file (system full path)
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Size of the entry on the disk
        /// </summary>
        long Size { get; }

        #endregion

        #region events

        /// <summary>
        /// Raises when the attributes are changed (IsCompressed, IsDevice, IsDirectory, ...);
        /// </summary>
        event TypedEventHandler<IFileSystemEntry, FileAttributes> AttributesChanged;

        /// <summary>
        /// Raised when the entry no longer exists on the disk (has been moved or deleted)
        /// </summary>
        event TypedEventHandler<IFileSystemEntry, ChangeEventArgs> Deleted;

        /// <summary>
        /// Fired when <see cref="Partial"/> is changed.
        /// </summary>
        event TypedEventHandler<IFileSystemEntry, bool> PartialChanged;

        /// <summary>
        /// Raised when renamed
        /// </summary>
        event TypedEventHandler<IFileSystemEntry, string> Renamed;

        /// <summary>
        /// Raised when the size changes 
        /// </summary>
        event TypedEventHandler<IFileSystemEntry, long> SizedChanged;

        #endregion

        #region methods

        /// <summary>
        /// Copy the file to a new directory
        /// </summary>
        /// <param name="newPath">The path to copy the file to</param>
        void CopyTo(string newPath);

        /// <summary>
        /// Deletes the file
        /// </summary>
        void Delete();

        /// <summary>
        /// Get the access controls for this entry
        /// </summary>
        /// <returns><see cref="FileSystemSecurity"/> associated with this entry</returns>
        FileSystemSecurity GetAccessControl();

        /// <summary>
        /// Move the file to a new directory
        /// </summary>
        /// <param name="newPath">The path to move the file to</param>
        /// <exception cref="IOException">Thrown when the entry cannot be moved.</exception>
        void Move(string newPath);

        /// <summary>
        /// Rename the file
        /// </summary>
        /// <param name="newName">The new name of the file</param>
        void Rename(string newName);

        void RefreshInfos();
        #endregion
    }
}
