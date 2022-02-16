using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.Data;

using MultitoolWinUI.Helpers;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class DriveInfoView : UserControl, INotifyPropertyChanged
    {
        private static readonly string[] sysFiles = new string[] { "pagefile.sys", "hiberfil.sys", "swapfile.sys" };
        private readonly Stopwatch stopwatch = new();
        private string _recycleBinSize = string.Empty;
        private double _recycleBinPercentage;
        private double _sysFilesPercentage;
        private long _sysFilesSize;
        private DriveInfo _driveInfo;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DriveInfoView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SetCompleted(false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            App.MainWindow.SizeChanged -= OnWindowSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Parent is ListView listview)
            {
                Width = listview.ActualWidth - 40;
                App.MainWindow.SizeChanged += OnWindowSizeChanged;
                Visibility = Visibility.Visible;
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            Width = (Parent as ListView).ActualWidth - 40;
        }

        public DriveInfoView(DriveInfo driveInfo, CancellationTokenSource cancelToken) : this()
        {
            InitializeComponent();
            _driveInfo = driveInfo;
            if (DriveInfo.IsReady)
            {
                cancelToken.Token.ThrowIfCancellationRequested();
                _ = LoadComponents(cancelToken);
            }
            else
            {
                App.TraceWarning("Drive could not be loaded. '" + driveInfo.Name + "' was not ready");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties

        public DriveInfo DriveInfo
        {
            get => _driveInfo;
            set
            {
                if (_driveInfo != null)
                {
                    _driveInfo = value;
                }
                else
                {
                    _driveInfo = value;
                    CancellationTokenSource cancelToken = new();
                    Unloaded += (object o, RoutedEventArgs e) => cancelToken?.Cancel();
                    _ = LoadComponents(cancelToken);
                }
            }
        }
        public string DriveName
        {
            get => DriveInfo == null ? string.Empty : DriveInfo.Name;
            set => DriveInfo = new(value);
        }
        public string VolumeLabel => DriveInfo == null ? string.Empty : DriveInfo.VolumeLabel;

        public string DriveCapacity => DriveInfo == null ? "0" : Tool.FormatSize(DriveInfo.TotalSize);

        public string DriveFreeSpace => DriveInfo == null ? "0" : Tool.FormatSize(DriveInfo.TotalFreeSpace);

        public string SysFilesSize => DriveInfo == null ? "0" : Tool.FormatSize(_sysFilesSize);

        public double DriveFreeSpacePercentage
        {
            get
            {
                if (DriveInfo != null)
                {
                    return (DriveInfo.TotalFreeSpace / (double)DriveInfo.TotalSize) * 100;
                }
                else
                {
                    return 0;
                }
            }
        }

        public string RecycleBinSize
        {
            get => _recycleBinSize;
            private set
            {
                _recycleBinSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecycleBinSize)));
            }
        }

        public double RecycleBinPercentage
        {
            get => _recycleBinPercentage;
            private set
            {
                _recycleBinPercentage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecycleBinPercentage)));
            }
        }

        public double SysFilesPercentage
        {
            get => _sysFilesPercentage;
            set
            {
                _sysFilesPercentage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SysFilesPercentage)));
            }
        }

        #endregion

        #region methods

        private async Task LoadComponents(CancellationTokenSource cancelTokenSource)
        {
            if (DriveInfo == null)
            {
                return;
            }

            CancellationToken cancelToken = cancelTokenSource.Token;
            cancelToken.ThrowIfCancellationRequested();

            long size = await Task.Run(() =>
            {
                return new DirectorySizeCalculator().CalculateDirectorySize(DriveInfo.Name + @"$RECYCLE.BIN\", cancelToken);
            }, cancelToken);

            _ = DispatcherQueue.TryEnqueue(() =>
            {
                RecycleBinSize = Tool.FormatSize(size);
                RecycleBinPercentage = (size / (double)DriveInfo.TotalSize) * 100;
                RecycleBin_TextBlock.Opacity = 1;
            });

            cancelToken.ThrowIfCancellationRequested();
            await GetStaticSysFilesSize(cancelToken);
            _ = DispatcherQueue.TryEnqueue(() => SysFiles_TextBlock.Opacity = 1);
            cancelTokenSource.Dispose();
        }

        private async Task GetStaticSysFilesSize(CancellationToken cancelToken)
        {
            await Task.Run(() =>
            {
                stopwatch.Start();

                for (int i = 0; i < sysFiles.Length; i++)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    if (File.Exists(DriveInfo.Name + sysFiles[i]))
                    {
                        _sysFilesSize += new FileInfo(DriveInfo.Name + sysFiles[i]).Length;
                    }
                }
                DisplaySysFileSize();
            }, cancelToken);

            cancelToken.ThrowIfCancellationRequested();
            await Task.Run(() => ComputeSysFiles(DriveInfo.Name, cancelToken), cancelToken);

            SetCompleted(true);
        }

        private void ComputeSysFiles(string path, CancellationToken cancelToken)
        {
            try
            {
                string[] dirs = Directory.GetDirectories(path);
                for (int i = 0; i < dirs.Length; i++)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    ComputeSysFiles(dirs[i], cancelToken);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }

            try
            {
                string[] files = Directory.GetFiles(path);
                FileInfo fileInfo;
                for (int i = 0; i < files.Length; i++)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    if (files[i].EndsWith(".sys"))
                    {
                        try
                        {
                            fileInfo = new FileInfo(files[i]);
                            _sysFilesSize += fileInfo.Length;
                            DisplaySysFileSize();
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (FileNotFoundException) { }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
        }

        private void DisplaySysFileSize()
        {
            if (stopwatch.ElapsedMilliseconds > 150)
            {
                stopwatch.Reset();
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SysFilesSize)));
                    SysFilesPercentage = (_sysFilesSize / (double)DriveInfo.TotalSize) * 100;
                });
                stopwatch.Start();
            }
        }

        private void SetCompleted(bool status)
        {
            if (status)
            {
                _ = DispatcherQueue.TryEnqueue(() =>
                  {
                      ProgressRing.IsIndeterminate = false;
                      ProgressRing.Visibility = Visibility.Collapsed;
                  });
            }
            else
            {
                _ = DispatcherQueue.TryEnqueue(() =>
                  {
                      ProgressRing.IsIndeterminate = true;
                      ProgressRing.Visibility = Visibility.Visible;
                  });
            }
        }

        #endregion
    }
}
