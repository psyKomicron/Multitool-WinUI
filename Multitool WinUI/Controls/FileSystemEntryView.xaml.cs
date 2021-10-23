using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.DAL;
using Multitool.DAL.FileSystem.Events;

using MultitoolWinUI.Helpers;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    [DebuggerDisplay("{Name}, {Path}")]
    public sealed partial class FileSystemEntryView : UserControl, IFileSystemEntry, INotifyPropertyChanged
    {
        private const ushort uiUpdateMs = 30;
        private const string DirectoryIcon = "📁";
        private const string FileIcon = "📄";
        private const string HiddenIcon = "👁";
        private const string SystemIcon = "⚙";
        private const string ReadOnlyIcon = "❌";
        private const string EncryptedIcon = "🔒";
        private const string CompressedIcon = "💾";
        private const string DeviceIcon = "‍💻";
        private readonly Stopwatch uiUpdateStopwatch = new();
        private volatile bool loaded;

        private string _displaySizeUnit;
        private string _displaySize;
        private string _partialIcon;
        private Brush _color;

        public FileSystemEntryView(IFileSystemEntry item)
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            App.MainWindow.SizeChanged += OnWindowSizeChanged;

            FileSystemEntry = item;

            item.AttributesChanged += OnAttributesChanged;
            item.Deleted += OnDeleted;
            item.SizedChanged += OnSizeChanged;
            item.PartialChanged += OnPartialChanged;

            Icon = GetIcon();

            Color = IsDirectory ? new SolidColorBrush(Tool.GetAppRessource<Windows.UI.Color>("DevBlue")) : new SolidColorBrush(Microsoft.UI.Colors.White);

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

        #region decoration
        public IFileSystemEntry FileSystemEntry { get; }

        public FileAttributes Attributes => FileSystemEntry.Attributes;

        public FileSystemInfo Info => FileSystemEntry.Info;

        public bool IsCompressed => FileSystemEntry.IsCompressed;

        public bool IsDevice => FileSystemEntry.IsDevice;

        public bool IsDirectory => FileSystemEntry.IsDirectory;

        public bool IsEncrypted => FileSystemEntry.IsEncrypted;

        public bool IsHidden
        {
            get => FileSystemEntry.IsHidden;
            set => FileSystemEntry.IsHidden = value;
        }
        public bool IsReadOnly
        {
            get => FileSystemEntry.IsReadOnly;
            set => FileSystemEntry.IsReadOnly = value;
        }

        public bool IsSystem => FileSystemEntry.IsSystem;

        public new string Name => FileSystemEntry.Name;

        public bool Partial => FileSystemEntry.Partial;

        public string Path => FileSystemEntry.Path;

        public long Size => FileSystemEntry.Size;

        public event TypedEventHandler<IFileSystemEntry, FileAttributes> AttributesChanged
        {
            add
            {
                ((IFileSystemEntry)FileSystemEntry).AttributesChanged += value;
            }

            remove
            {
                ((IFileSystemEntry)FileSystemEntry).AttributesChanged -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, ChangeEventArgs> Deleted
        {
            add
            {
                ((IFileSystemEntry)FileSystemEntry).Deleted += value;
            }

            remove
            {
                ((IFileSystemEntry)FileSystemEntry).Deleted -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, bool> PartialChanged
        {
            add
            {
                ((IFileSystemEntry)FileSystemEntry).PartialChanged += value;
            }

            remove
            {
                ((IFileSystemEntry)FileSystemEntry).PartialChanged -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, string> Renamed
        {
            add
            {
                ((IFileSystemEntry)FileSystemEntry).Renamed += value;
            }

            remove
            {
                ((IFileSystemEntry)FileSystemEntry).Renamed -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, long> SizedChanged
        {
            add
            {
                ((IFileSystemEntry)FileSystemEntry).SizedChanged += value;
            }

            remove
            {
                ((IFileSystemEntry)FileSystemEntry).SizedChanged -= value;
            }
        }

        public int CompareTo(object obj)
        {
            return FileSystemEntry.CompareTo(obj);
        }

        public int CompareTo(IFileSystemEntry other)
        {
            return FileSystemEntry.CompareTo(other);
        }

        public void CopyTo(string newPath)
        {
            FileSystemEntry.CopyTo(newPath);
        }

        public void Delete()
        {
            FileSystemEntry.Delete();
        }

        public bool Equals(IFileSystemEntry other)
        {
            return FileSystemEntry.Equals(other);
        }

        public void Move(string newPath)
        {
            FileSystemEntry.Move(newPath);
        }

        public void Rename(string newName)
        {
            FileSystemEntry.Rename(newName);
        }

        public FileSystemSecurity GetAccessControl()
        {
            return FileSystemEntry.GetAccessControl();
        }

        public void RefreshInfos()
        {
            FileSystemEntry.RefreshInfos();
        }
        #endregion

        #region view
        public ListView ListView { get; set; }

        public Page Page { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        #region private

        private void RaiseNotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            _ = DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)));
        }

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

        #endregion

        #region events handlers

        #region filesystementry events

        private void OnPartialChanged(IFileSystemEntry sender, bool args)
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
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                Color.Opacity = 1;
                PartialIcon = null;
            });
            RaiseNotifyPropertyChanged(nameof(Partial));
        }

        private void OnSizeChanged(IFileSystemEntry sender, long newSize)
        {
            Tool.FormatSize(Size, out double formatted, out string ext);
            _displaySizeUnit = ext;
            _displaySize = formatted.ToString("F2", CultureInfo.InvariantCulture);
            if (uiUpdateStopwatch.ElapsedMilliseconds > uiUpdateMs)
            {
                RaiseNotifyPropertyChanged(nameof(DisplaySize) + " " + nameof(DisplaySizeUnit));
                uiUpdateStopwatch.Restart();
            }
        }

        private void OnDeleted(IFileSystemEntry sender, ChangeEventArgs e)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                Color = new SolidColorBrush(Microsoft.UI.Colors.Red)
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

        #region control events
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
            Width = ListView == null ? double.NaN : ListView.ActualWidth - 40;
            //App.MainWindow.SizeChanged += OnWindowSizeChanged;
            Page.SizeChanged += OnPageChanged;
        }

        private void OnPageChanged(object sender, SizeChangedEventArgs e)
        {
            if (!loaded) return;
            Width = ListView == null ? double.NaN : ListView.ActualWidth - 40;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            loaded = false;
            App.MainWindow.SizeChanged -= OnWindowSizeChanged;
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            if (!loaded) return;
            Width = ListView == null ? double.NaN : ListView.ActualWidth - 40;
        }
        #endregion

        #endregion

    }
}
