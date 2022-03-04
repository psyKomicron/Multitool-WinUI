using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Data;
using Multitool.Data.FileSystem.Events;

using MultitoolWinUI.Helpers;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class FileSystemEntryView : UserControl, IFileSystemEntry, INotifyPropertyChanged
    {
        // replace those fields with FontIcons
        private const ushort uiUpdateMs = 120;
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
            FileSystemEntry = item;
            Icon = GetIcon();
            AddAttributes();
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

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            item.AttributesChanged += OnAttributesChanged;
            item.Deleted += OnDeleted;
            item.SizedChanged += OnSizeChanged;
            item.PartialChanged += Item_PartialChanged;
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
                FileSystemEntry.AttributesChanged += value;
            }

            remove
            {
                FileSystemEntry.AttributesChanged -= value;
            }
        }

        public event TypedEventHandler<IFileSystemEntry, ChangeEventArgs> Deleted
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

        public int CompareTo(object obj)
        {
            return FileSystemEntry.CompareTo(obj);
        }

        #endregion

        #region view properties
        public Brush Color
        {
            get => _color;
            set
            {
                _color = value;
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
        public string DisplaySize
        {
            get => _displaySize;
            set
            {
                _displaySize = value;
                RaiseNotifyPropertyChanged();
            }
        }
        public bool IsFile => !IsDirectory;
        public string Icon { get; }
        public string IsHiddenEcon => FileSystemEntry.IsHidden ? HiddenIcon : string.Empty;
        public string IsSystemEcon => FileSystemEntry.IsSystem ? SystemIcon : string.Empty;
        public string IsReadOnlyEcon => FileSystemEntry.IsReadOnly ? ReadOnlyIcon : string.Empty;
        public string IsEncryptedEcon => FileSystemEntry.IsEncrypted ? EncryptedIcon : string.Empty;
        public string IsCompressedEcon => FileSystemEntry.IsCompressed ? CompressedIcon : string.Empty;
        public string IsDeviceEcon => FileSystemEntry.IsDevice ? DeviceIcon : string.Empty;
        public ListView ListView { get; set; }
        public Page Page { get; set; }
        public string PartialIcon
        {
            get => _partialIcon;
            set
            {
                _partialIcon = value;
                RaiseNotifyPropertyChanged();
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        #region public methods
        public Task<Windows.Storage.IStorageItem> AsIStorageItem()
        {
            return FileSystemEntry.AsIStorageItem();
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

        #region private
        private static TextBlock CreateAttributeTextBlock(string text, string tooltip)
        {
            TextBlock textBlock = new()
            {
                Text = text
            };
            ToolTipService.SetToolTip(textBlock, tooltip);
            return textBlock;
        }

        private void RaiseNotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            _ = DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new(propName)));
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

        private void AddAttributes()
        {
            if (IsHidden)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(HiddenIcon, "Hidden"));
            }
            if (IsSystem)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(SystemIcon, "System"));
            }
            if (IsReadOnly)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(ReadOnlyIcon, "Read-only"));
            }
            if (IsEncrypted)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(EncryptedIcon, "Encrypted"));
            }
            if (IsCompressed)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(CompressedIcon, "Compressed"));
            }
            if (IsDevice)
            {
                AttributesGrid.Children.Add(CreateAttributeTextBlock(DeviceIcon, "Device"));
            }
        }

        private void UpdateControlSize()
        {
            if (!loaded) return;
            Width = ListView == null ? double.NaN : ListView.ActualWidth - 40;
        }
        #endregion

        #region events handlers

        #region filesystementry events
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
            _ = DispatcherQueue.TryEnqueue(() =>
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
            UpdateControlSize();
            App.MainWindow.SizeChanged += OnWindowSizeChanged;
            Page.SizeChanged += OnPageChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            loaded = false;
            App.MainWindow.SizeChanged -= OnWindowSizeChanged;
        }

        private void OnPageChanged(object sender, SizeChangedEventArgs e) => UpdateControlSize();

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args) => UpdateControlSize();
        #endregion

        #endregion
    }
}
