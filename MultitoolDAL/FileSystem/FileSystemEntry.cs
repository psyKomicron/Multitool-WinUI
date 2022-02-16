using Multitool.Data.FileSystem.Events;

using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Data
{
    /// <summary>
    /// Base class for directory and file entries
    /// </summary>
    public abstract class FileSystemEntry : IFileSystemEntry
    {
        private bool _partial = true;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        protected FileSystemEntry(FileSystemInfo info)
        {
            Path = info.FullName;
            Name = info.Name;
            Info = info;
        }

        #region properties
        /// <inheritdoc/>
        public abstract long Size { get; set; }

        /// <inheritdoc/>
        public FileSystemInfo Info { get; protected set; }

        /// <inheritdoc/>
        public FileAttributes Attributes => Info.Attributes;

        /// <inheritdoc/>
        public bool IsHidden
        {
            get => (Attributes & FileAttributes.Hidden) != 0;
            set
            {
                if (!value)
                {
                    RemoveIsHidden();
                }
                else
                {
                    SetHidden();
                }
            }
        }
        /// <inheritdoc/>
        public bool IsSystem => (Attributes & FileAttributes.System) != 0;

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get => (Attributes & FileAttributes.ReadOnly) != 0;
            set
            {
                if (!value)
                {
                    RemoveReadOnly();
                }
                else
                {
                    SetReadOnly();
                }
            }
        }

        /// <inheritdoc/>
        public bool IsEncrypted => (Attributes & FileAttributes.Encrypted) != 0;

        /// <inheritdoc/>
        public bool IsCompressed => (Attributes & FileAttributes.Compressed) != 0;

        /// <inheritdoc/>
        public bool IsDevice => (Attributes & FileAttributes.Device) != 0;

        /// <inheritdoc/>
        public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;

        /// <inheritdoc/>
        public string Path { get; set; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public bool Partial
        {
            get => _partial;
            set
            {
                _partial = value;
#if false
                _ = Task.Run(() => PartialChanged?.Invoke(this, value));
#else
                PartialChanged?.Invoke(this, value);
#endif
            }
        }
        #endregion

        #region events
        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, ChangeEventArgs> Deleted;

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, long> SizedChanged;

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, FileAttributes> AttributesChanged;

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, string> Renamed;

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, bool> PartialChanged;
        #endregion

        #region abstract methods

        /// <inheritdoc/>
        public abstract void CopyTo(string newPath);

        /// <inheritdoc/>
        public abstract FileSystemSecurity GetAccessControl();

        /// <inheritdoc/>
        public abstract void Move(string newPath);

        /// <inheritdoc/>
        public abstract void RefreshInfos();

        /// <inheritdoc/>
        public abstract void Rename(string newName);

        #endregion

        #region public methods
        /// <inheritdoc/>
        public virtual void Delete()
        {
            if (CanDelete())
            {
                Info.Delete();
                RaiseDeletedEvent();
            }
            else
            {
                throw CreateDeleteIOException();
            }
        }

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            if (obj is FileSystemEntry that)
            {
                return CompareTo(that);
            }
            else
            {
                return 0;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(IFileSystemEntry other)
        {
            if (IsDirectory && !other.IsDirectory)
            {
                return -1;
            }
            if (!IsDirectory && other.IsDirectory)
            {
                return 1;
            }

            if (Size > other.Size)
            {
                return -1;
            }
            if (Size < other.Size)
            {
                return 1;
            }
            return 0;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is not null && Equals(obj as IFileSystemEntry));
        }

        /// <inheritdoc/>
        public bool Equals(IFileSystemEntry other)
        {
            return other != null && Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Info.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name + ", " + Path;
        }

        #region operators
        /// <inheritdoc/>
        public static bool operator ==(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FileSystemEntry left, FileSystemEntry right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(FileSystemEntry left, FileSystemEntry right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #endregion

        #region protected methods
        /// <summary>
        /// Set path and name of this <see cref="FileSystemEntry"/>. Use after refreshing info.
        /// </summary>
        protected void SetInfos(FileSystemInfo newInfo)
        {
            Info = newInfo;
            Path = Info.FullName;
            Name = Info.Name;
        }

        /// <summary>
        /// Checks if a <see cref="FileSystemEntry"/> can be moved and if not, why.
        /// </summary>
        /// <param name="newPath"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        protected virtual bool CanMove(string newPath, out MoveCodes res)
        {
            if (File.Exists(newPath))
            {
                if (IsSystem)
                {
                    res = MoveCodes.IsSystem;
                    return false;
                }
                else if (Info == null)
                {
                    res = MoveCodes.InfoNotSet;
                    return false;
                }
                else
                {
                    res = MoveCodes.Possible;
                    return true;
                }
            }
            else
            {
                res = MoveCodes.PathNotFound;
                return false;
            }
        }

        /// <summary>
        /// Checks if an entry can be renamed.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        protected virtual bool CanRename(string newName)
        {
            if (!File.Exists(Path))
            {
                if (IsSystem)
                {
                    //res = MoveCodes.IsSystem;
                    return false;
                }
                else if (Info == null)
                {
                    //res = MoveCodes.InfoNotSet;
                    return false;
                }
                else
                {
                    //res = MoveCodes.Possible;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if <paramref name="fileInfo"/> can be deleted.
        /// </summary>
        /// <param name="fileInfo">File to delete</param>
        /// <returns><see langword="true"/> if the file can be deleted</returns>
        protected virtual bool CanDelete(FileSystemInfo fileInfo)
        {
            if (((fileInfo.Attributes & FileAttributes.Device) != 0) || ((fileInfo.Attributes & FileAttributes.System) != 0))
            {
                if (fileInfo.Name == "desktop.ini")
                {
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        RemoveReadOnly(fileInfo);
                    }
                    return true;
                }

                return false;
            }

            if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
            {
                RemoveReadOnly(fileInfo);
            }
            return true;
        }

        /// <summary>
        /// Checks if the entry can be deleted.
        /// </summary>
        /// <returns><see langword="true"/> if the file can be deleted</returns>
        protected virtual bool CanDelete()
        {
            return CanDelete(Info);
        }

        /// <summary>
        /// Creates a <see cref="IOException"/> when the entry cannot be
        /// deleted.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        protected static IOException CreateDeleteIOException(FileSystemInfo info)
        {
            IOException e = new("Cannot delete " + info.FullName);
            e.Data.Add(info.ToString(), info);
            return e;
        }

        /// <summary>
        /// Creates a <see cref="IOException"/> when the entry cannot be
        /// deleted.
        /// </summary>
        /// <returns></returns>
        protected IOException CreateDeleteIOException()
        {
            return CreateDeleteIOException(Info);
        }

        /// <summary>
        /// Removes the readonly attribute.
        /// </summary>
        protected void RemoveReadOnly()
        {
            Info.Attributes &= ~FileAttributes.ReadOnly;
            RaiseAttributesChangedEvent(FileAttributes.ReadOnly);
        }

        /// <summary>
        /// Removes the readonly attribute on <paramref name="info"/>.
        /// </summary>
        protected static void RemoveReadOnly(FileSystemInfo info)
        {
            info.Attributes &= ~FileAttributes.ReadOnly;
        }

        /// <summary>
        /// Removes the hidden attribute.
        /// </summary>
        protected void RemoveIsHidden()
        {
            Info.Attributes &= ~FileAttributes.ReadOnly;
            RaiseAttributesChangedEvent(FileAttributes.Hidden);
        }

        /// <summary>
        /// Adds a readonly attribute.
        /// </summary>
        protected void SetReadOnly()
        {
            Info.Attributes |= FileAttributes.ReadOnly;
        }

        /// <summary>
        /// Adds a hidden attribute.
        /// </summary>
        protected void SetHidden()
        {
            Info.Attributes |= FileAttributes.Hidden;
        }

        #region event invoke
        /// <summary>
        /// <see langword="protected internal"/>
        /// </summary>
        protected void RaiseDeletedEvent()
        {
            Deleted?.Invoke(this, new ChangeEventArgs(this, ChangeTypes.FileDeleted));
        }

        /// <summary>
        /// <see langword="protected internal"/>
        /// </summary>
        /// <param name="oldSize"></param>
        protected void RaiseSizeChangedEvent(long oldSize)
        {
            Task.Run(() => SizedChanged?.Invoke(this, oldSize));
        }

        /// <summary>
        /// <see langword="protected internal"/>
        /// </summary>
        /// <param name="attributes"></param>
        protected void RaiseAttributesChangedEvent(FileAttributes attributes)
        {
            AttributesChanged?.Invoke(this, attributes);
        }

        /// <summary>
        /// <see langword="protected internal"/>
        /// </summary>
        /// <param name="oldPath"></param>
        protected void RaiseRenamedEvent(string oldPath)
        {
            Renamed?.Invoke(this, oldPath);
        }
        #endregion

        #endregion
    }
}
