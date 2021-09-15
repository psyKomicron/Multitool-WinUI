using Multitool.DAL.Events;

using System;
using System.IO;
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
        public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;

        /// <inheritdoc/>
        public bool IsSystem => (Attributes & FileAttributes.System) != 0;

        /// <inheritdoc/>
        public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;

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
                PartialChanged?.Invoke(this, value);
            }
        }

        #endregion

        #region events

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemEntry, FileChangeEventArgs> Deleted;

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
        public abstract void Rename(string newName);

        public abstract void CopyTo(string newPath);

        /// <inheritdoc/>
        public abstract void Move(string newPath);

        /// <inheritdoc/>
        public abstract void RefreshInfos();

        #endregion

        #region public methods

        /// <inheritdoc/>
        public virtual void Delete()
        {
            if (CanDelete())
            {
                Info.Delete();
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
        public bool Equals(IFileSystemEntry other)
        {
            return other != null && Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name + ", " + Path;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is null ? false : Equals(obj as IFileSystemEntry));
        }

        public override int GetHashCode()
        {
            return Info.GetHashCode();
        }

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

        protected void RemoveReadOnly(FileSystemInfo info)
        {
            info.Attributes &= ~FileAttributes.ReadOnly;
            AttributesChanged?.Invoke(this, FileAttributes.ReadOnly);
        }

        protected void RaiseDeletedEvent()
        {
            Deleted?.Invoke(this, new FileChangeEventArgs(this, WatcherChangeTypes.Deleted));
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

        #region events

        #region watcher events
        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            RaiseDeletedEvent();
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Path = e.FullPath;
            Name = e.Name;
            RaiseRenamedEvent(e.OldFullPath);
        }
        #endregion

        #endregion
    }
}
