using Multitool.DAL.FileSystem.Events;

using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.DAL
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
                return 1;
            }
            if (!IsDirectory && other.IsDirectory)
            {
                return -1;
            }

            if (Size > other.Size)
            {
                return 1;
            }
            if (Size < other.Size)
            {
                return -1;
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

        public bool Equals(FileSystemInfo info)
        {
            return Path.Equals(info.FullName, StringComparison.OrdinalIgnoreCase) &&
                Attributes == info.Attributes;
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
        public static bool operator ==(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(FileSystemEntry left, FileSystemEntry right)
        {
            return !(left == right);
        }

        public static bool operator <(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        public static bool operator <=(FileSystemEntry left, FileSystemEntry right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(FileSystemEntry left, FileSystemEntry right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

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

        protected virtual bool CanDelete()
        {
            return CanDelete(Info);
        }

        protected static IOException CreateDeleteIOException(FileSystemInfo info)
        {
            IOException e = new("Cannot delete " + info.FullName);
            e.Data.Add(info.ToString(), info);
            return e;
        }

        protected IOException CreateDeleteIOException()
        {
            return CreateDeleteIOException(Info);
        }

        protected void RemoveReadOnly()
        {
            Info.Attributes &= ~FileAttributes.ReadOnly;
            RaiseAttributesChangedEvent(FileAttributes.ReadOnly);
        }

        protected static void RemoveReadOnly(FileSystemInfo info)
        {
            info.Attributes &= ~FileAttributes.ReadOnly;
        }

        protected void RemoveIsHidden()
        {
            Info.Attributes &= ~FileAttributes.ReadOnly;
            RaiseAttributesChangedEvent(FileAttributes.Hidden);
        }

        protected void SetReadOnly()
        {
            Info.Attributes |= FileAttributes.ReadOnly;
        }

        protected void SetHidden()
        {
            Info.Attributes |= FileAttributes.Hidden;
        }

        #region event invoke
        protected void RaiseDeletedEvent()
        {
            Deleted?.Invoke(this, new ChangeEventArgs(this, ChangeTypes.FileDeleted));
        }

        protected void RaiseSizeChangedEvent(long oldSize)
        {
            Task.Run(() => SizedChanged?.Invoke(this, oldSize));
        }

        protected void RaiseAttributesChangedEvent(FileAttributes attributes)
        {
            AttributesChanged?.Invoke(this, attributes);
        }

        protected void RaiseRenamedEvent(string oldPath)
        {
            Renamed?.Invoke(this, oldPath);
        }
        #endregion

        #endregion
    }
}
