using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;

namespace Multitool.DAL
{
    internal class DirectoryEntry : FileSystemEntry
    {
        public long _size;
        private DirectoryInfo dirInfo;

        #region ctors
        public DirectoryEntry(DirectoryInfo info) : base(info)
        {
            dirInfo = info;
        }

        public DirectoryEntry(DirectoryInfo info, long size) : base(info)
        {
            dirInfo = info;
            Size = size;
        }
        #endregion

        /// <inheritdoc/>
        public override long Size
        {
            get => _size;
            set
            {
                if (!Partial)
                {
                    throw new InvalidOperationException("Item is not partial");
                }
                long old = _size;
                _size = value;
                RaiseSizeChangedEvent(old);
            }
        }

        #region public methods

        /// <inheritdoc/>
        public override void CopyTo(string newPath)
        {
            if (Directory.Exists(newPath))
            {
                if (IsSystem)
                {
                    throw new IOException("Cannot move system file");
                }
                else
                {
                    DirectoryCopy(dirInfo, newPath);
                }
            }
        }

        /// <inheritdoc/>
        public override void Delete()
        {
            if (CanDelete())
            {
                DeleteDir(Path);
            }
            else
            {
                throw CreateDeleteIOException();
            }
        }

        public override FileSystemSecurity GetAccessControl()
        {
            return dirInfo.GetAccessControl();
        }

        /// <inheritdoc/>
        public override void Move(string newPath)
        {
            if (CanMove(newPath, out MoveCodes res))
            {
                dirInfo.MoveTo(newPath);
            }
            else
            {
                string message = res switch
                {
                    MoveCodes.PathNotFound => "path not found",
                    MoveCodes.IsSystem => "file/directory belongs to the system",
                    MoveCodes.InfoNotSet => throw new InvalidOperationException("IO actions cannot be performed until the entry has a reference to a FileSystemInfo"),
                    _ => throw new ArgumentException("MoveCodes not recognized"),
                };
                Trace.TraceError(Name + " cannot be moved: " + message);
                throw new IOException(Path + " cannot be moved (reason: " + message + ")");
            }
        }

        public override void Rename(string newName)
        {
            if (CanRename(newName))
            {
                throw new NotImplementedException();
            }
        }

        public override void RefreshInfos()
        {
            RefreshInfosAsync();
        }
        #endregion

        #region private methods
        private void DirectoryCopy(DirectoryInfo info, string newPath)
        {
            DirectoryInfo[] dirs = info.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            DirectoryInfo newDir = Directory.CreateDirectory(newPath);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = System.IO.Path.Combine(newDir.FullName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy them and their contents to new location.
            for (int i = 0; i < dirs.Length; i++)
            {
                string tempPath = System.IO.Path.Combine(newDir.FullName, dirs[i].Name);
                DirectoryCopy(dirs[i], tempPath);
            }
        }

        private void DeleteDir(string dirPath)
        {
            string[] dirs = Directory.GetDirectories(dirPath);
            for (int i = 0; i < dirs.Length; i++)
            {
                DeleteDir(dirs[i]);
            }

            FileInfo fileInfo;
            string[] files = Directory.GetFiles(dirPath);
            for (int i = 0; i < files.Length; i++)
            {
                fileInfo = new FileInfo(files[i]);
                if (CanDelete(fileInfo))
                {
                    fileInfo.Delete();
                }
                else
                {
                    throw CreateDeleteIOException(fileInfo);
                }
            }

            DirectoryInfo directoryInfo = new(dirPath);
            if (CanDelete(directoryInfo))
            {
                directoryInfo.Delete();
            }
            else
            {
                throw CreateDeleteIOException(directoryInfo);
            }
        }

        private async void RefreshInfosAsync()
        {
            string oldPath = Path;
            dirInfo.Refresh();
            if (!dirInfo.Exists)
            {
                dirInfo = new DirectoryInfo(oldPath);
            }
            SetInfos(dirInfo);
            Partial = true;
            Size = await new DirectorySizeCalculator().CalculateDirectorySizeAsync(Path, CancellationToken.None);
            Partial = false;
        }
        #endregion
    }
}
