using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

using Multitool.FileSystem;
using Multitool.FileSystem.Events;

using MultitoolWinUI.Helpers;

using System.Diagnostics;
using System.Globalization;
using System.IO;

using Windows.Foundation;
using Windows.UI;

namespace MultitoolWinUI.Models
{
    public class FileSystemEntryViewModel : ViewModel, IFileSystemEntry
    {
        private const ushort uiUpdateMs = 50;
        private const string DirectoryIcon = "📁";
        private const string FileIcon = "📄";
        private const string HiddenIcon = "👁";
        private const string SystemIcon = "⚙";
        private const string ReadOnlyIcon = "❌";
        private const string EncryptedIcon = "🔒";
        private const string CompressedIcon = "💾";
        private const string DeviceIcon = "‍💻";

        private Stopwatch uiUpdateStopwatch = new();

        private string _displaySizeUnit;
        private string _displaySize;
        private string _partialIcon;
        private Brush _color;

        /// <summary>Constructor.</summary>
        /// <param name="item"><see cref="IFileSystemEntry"/> to decorate</param>
        public FileSystemEntryViewModel(IFileSystemEntry item, DispatcherQueue dispatcherQueue) : base(dispatcherQueue)
        {
            FileSystemEntry = item;

            item.AttributesChanged += OnAttributesChanged;
            item.Deleted += OnDeleted;
            item.SizedChanged += OnSizeChanged;
            item.PartialChanged += Item_PartialChanged;

            Icon = GetIcon();

            Color = IsDirectory ? new SolidColorBrush(Tool.GetAppRessource<Color>("DevBlue")) : new SolidColorBrush(Colors.White);

            if (Partial)
            {
                PartialIcon = "\xe783";
                Color.Opacity = 0.6;
                DisplaySize = string.Empty;
            }
            else
            {
                Tool.FormatSize(Size, out double formatted, out string ext);
                DisplaySizeUnit = ext;
                DisplaySize = formatted.ToString("F2", CultureInfo.InvariantCulture);
            }

            uiUpdateStopwatch.Start();
        }

        #region properties

        #region IFileSystemEntry
        public FileAttributes Attributes => FileSystemEntry.Attributes;
        public IFileSystemEntry FileSystemEntry { get; }
        public FileSystemInfo Info => FileSystemEntry.Info;
        public string Path => FileSystemEntry.Path;
        public string Name => FileSystemEntry.Name;
        public bool IsHidden => FileSystemEntry.IsHidden;
        public bool IsSystem => FileSystemEntry.IsSystem;
        public bool IsReadOnly => FileSystemEntry.IsReadOnly;
        public bool IsEncrypted => FileSystemEntry.IsEncrypted;
        public bool IsCompressed => FileSystemEntry.IsCompressed;
        public bool IsDevice => FileSystemEntry.IsDevice;
        public bool IsDirectory => FileSystemEntry.IsDirectory;
        public long Size => FileSystemEntry.Size;
        public bool Partial => FileSystemEntry.Partial;
        #endregion

        public string Icon { get; }

        public string PartialIcon
        {
            get => _partialIcon;
            set
            {
                _partialIcon = value;
                RaiseNotifyPropertyChanged();
            }
        }

        public string IsHiddenEcon => FileSystemEntry.IsHidden ? HiddenIcon : string.Empty;

        public string IsSystemEcon => FileSystemEntry.IsSystem ? SystemIcon : string.Empty;

        public string IsReadOnlyEcon => FileSystemEntry.IsReadOnly ? ReadOnlyIcon : string.Empty;

        public string IsEncryptedEcon => FileSystemEntry.IsEncrypted ? EncryptedIcon : string.Empty;

        public string IsCompressedEcon => FileSystemEntry.IsCompressed ? CompressedIcon : string.Empty;

        public string IsDeviceEcon => FileSystemEntry.IsDevice ? DeviceIcon : string.Empty;

        public Brush Color
        {
            get => _color;
            set
            {
                _color = value;
                RaiseNotifyPropertyChanged();
            }
        }

        public string DisplaySize
        {
            get => _displaySize;
            set
            {
                _displaySize = value;
                RaiseNotifyPropertyChanged();
            }
        }

        public string DisplaySizeUnit
        {
            get => _displaySizeUnit;
            set
            {
                _displaySizeUnit = value;
                RaiseNotifyPropertyChanged();
            }
        }

        #endregion

        #region events

        public event TypedEventHandler<IFileSystemEntry, FileAttributes> AttributesChanged
        {
            add
            {
                FileSystemEntry.AttributesChanged += value;
            }

            remove
            {
                FileSystemEntry.AttributesChanged -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, FileChangeEventArgs> Deleted
        {
            add
            {
                FileSystemEntry.Deleted += value;
            }

            remove
            {
                FileSystemEntry.Deleted -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, string> Renamed
        {
            add
            {
                FileSystemEntry.Renamed += value;
            }

            remove
            {
                FileSystemEntry.Renamed -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, long> SizedChanged
        {
            add
            {
                FileSystemEntry.SizedChanged += value;
            }

            remove
            {
                FileSystemEntry.SizedChanged -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, bool> PartialChanged
        {
            add
            {
                FileSystemEntry.PartialChanged += value;
            }

            remove
            {
                FileSystemEntry.PartialChanged -= value;
            }
        }

        #endregion

        #region public
        ///<inheritdoc/>
        public int CompareTo(IFileSystemEntry other)
        {
            return FileSystemEntry.CompareTo(other);
        }

        ///<inheritdoc/>
        public int CompareTo(object obj)
        {
            return FileSystemEntry.CompareTo(obj);
        }

        ///<inheritdoc/>
        public bool Equals(IFileSystemEntry other)
        {
            return FileSystemEntry.Equals(other);
        }

        ///<inheritdoc/>
        public void Delete()
        {
            FileSystemEntry.Delete();
        }

        ///<inheritdoc/>
        public void Rename(string newName)
        {
            FileSystemEntry.Rename(newName);
        }

        ///<inheritdoc/>
        public void Move(string newPath)
        {
            FileSystemEntry.Move(newPath);
        }

        ///<inheritdoc/>
        public void CopyTo(string newPath)
        {
            FileSystemEntry.CopyTo(newPath);
        }
        #endregion

        #region private

        private string GetIcon()
        {
            return Name switch
            {
                "$RECYCLE.BIN" => "🗑",
                "desktop.ini" => "🖥",
                "swapfile.sys" or "hiberfil.sys" or "pagefile.sys" => "⚙",
                _ => IsDirectory ? DirectoryIcon : FileIcon,
            };
        }

        private void Item_PartialChanged(IFileSystemEntry sender, bool args)
        {
            if (Partial)
            {
                uiUpdateStopwatch.Start();
            }
            else
            {
                uiUpdateStopwatch.Stop();
            }

            Tool.FormatSize(Size, out double formatted, out string ext);
            DisplaySizeUnit = ext;
            DisplaySize = formatted.ToString("F2", CultureInfo.InvariantCulture);
            _ = CurrentDispatcherQueue.TryEnqueue(() =>
            {
                Color.Opacity = 1;
                PartialIcon = null;
            });
            RaiseNotifyPropertyChanged(nameof(Partial));
        }

        private void OnSizeChanged(IFileSystemEntry sender, long newSize)
        {
            if (!Partial)
            {
                Tool.FormatSize(Size, out double formatted, out string ext);
                DisplaySizeUnit = ext;
                DisplaySize = formatted.ToString("F2", CultureInfo.InvariantCulture);
            }
            else if (uiUpdateStopwatch.ElapsedMilliseconds > uiUpdateMs)
            {
                Tool.FormatSize(Size, out double formatted, out string ext);
                DisplaySizeUnit = ext;
                DisplaySize = formatted.ToString("F2", CultureInfo.InvariantCulture);
                uiUpdateStopwatch.Restart();
            }
        }

        private void OnDeleted(IFileSystemEntry sender, FileChangeEventArgs e)
        {
            _ = CurrentDispatcherQueue.TryEnqueue(() =>
            {
                Color = new SolidColorBrush(Colors.Red)
                {
                    Opacity = 0.8
                };
            });
        }

        private void OnAttributesChanged(IFileSystemEntry sender, FileAttributes attributes)
        {
            switch (attributes)
            {
                case FileAttributes.ReadOnly:
                    RaiseNotifyPropertyChanged(nameof(IsReadOnly));
                    break;
                case FileAttributes.Hidden:
                    RaiseNotifyPropertyChanged(nameof(IsHidden));
                    break;
                case FileAttributes.System:
                    RaiseNotifyPropertyChanged(nameof(IsSystem));
                    break;
                case FileAttributes.Directory:
                    RaiseNotifyPropertyChanged(nameof(IsDirectory));
                    break;
                case FileAttributes.Device:
                    RaiseNotifyPropertyChanged(nameof(IsDevice));
                    break;
                case FileAttributes.Compressed:
                    RaiseNotifyPropertyChanged(nameof(IsCompressed));
                    break;
                case FileAttributes.Encrypted:
                    RaiseNotifyPropertyChanged(nameof(IsEncrypted));
                    break;
            }
        }

        #endregion
    }

}
