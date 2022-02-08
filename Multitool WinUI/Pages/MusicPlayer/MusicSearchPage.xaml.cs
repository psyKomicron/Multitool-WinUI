using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL;
using Multitool.DAL.Settings;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.FileProperties;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicSearchPage : Page, INotifyPropertyChanged
    {
        private static readonly Regex audioMimeRegex = new(@"^audio/.+");
        private readonly string[] audioExtensions;
        private static readonly Regex ignoreList = new(@"(\$Recycle\.Bin)|(\$WinREAgent)|(Config\.Msi)|(ESD)|(Microsoft)|(PerfLogs)|(platform-tools)|(ProgramData)|(Recovery)|(System Volume Information)|(Temp)|(Windows)");

        public MusicSearchPage()
        {
            InitializeComponent();
            try
            {
                audioExtensions = RegistryHelper.GetExtensionsForMime(audioMimeRegex);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        [Setting(true)]
        public bool ShowThumbnails { get; set; }
        [Setting(90u)]
        public uint ThumbnailSize { get; set; }
        [Setting(true)]
        public bool SkipSmallFiles { get; set; }
        public int MinimumFileDuration { get; set; } = 10;

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.Settings.Load(this);
            PropertyChanged?.Invoke(this, new(string.Empty));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.Settings.Save(this);
            base.OnNavigatedFrom(e);
        }

        #region private methods
        private async Task LoadFiles()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            List<Task> tasks = new();
#if true
            for (int i = 0; i < drives.Length; i++)
            {
                DriveInfo drive = drives[i];
                DirectoryInfo[] folders = drive.RootDirectory.GetDirectories();
                foreach (var folder in folders)
                {
                    if (!ignoreList.IsMatch(folder.Name))
                    {
                        await LoadFolder(folder);
                    }
                }
            }
#else
            DriveInfo drive = drives[2];
            DirectoryInfo[] folders = drive.RootDirectory.GetDirectories();
            foreach (var folder in folders)
            {
                if (!ignoreList.IsMatch(folder.Name))
                {
                    tasks.Add(LoadFolder(folder));
                }
            }
#endif
            await Task.WhenAll(tasks);

            DispatcherQueue.TryEnqueue(() =>
            {
                FileLoadingProgress.IsIndeterminate = false;
                FileLoadingProgress.Visibility = Visibility.Collapsed;
            });
        }

        private async Task LoadFolder(DirectoryInfo root)
        {
            try
            {
                List<Task<MusicFileView>> fileTasks = new();
                List<Task> tasks = new();

                List<MusicFileView> views = new();
                List<string> validFiles = new();

                var files = root.GetFiles();
                if (files.Length > 0)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            for (int j = 0; j < audioExtensions.Length; j++)
                            {
                                if (audioExtensions[j] == files[i].Extension)
                                {
                                    validFiles.Add(files[i].FullName);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    // multi-threading for thumbnails
                    if (validFiles.Count > 0)
                    {
                        for (int i = 0; i < validFiles.Count; i++)
                        {
                            fileTasks.Add(CreateAddView(validFiles[i]));
                        }
                        MusicFileView[] fileList = await Task.WhenAll(fileTasks);
                        _ = DispatcherQueue.TryEnqueue(() =>
                        {
                            for (int i = 0; i < fileList.Length; i++)
                            {
                                if (fileList[i] != null)
                                {
                                    musicListView.Items.Add(fileList[i]);
                                }
                            }
                        });
                    }
                }

                DirectoryInfo[] folders = root.GetDirectories();
                for (int i = 0; i < folders.Length; i++)
                {
                    tasks.Add(LoadFolder(folders[i]));
                }
                await Task.WhenAll(tasks);
            }
            catch
            {
            }
        }

        private async Task<MusicFileView> CreateAddView(string fullPath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(fullPath);
            var props = await file.Properties.GetMusicPropertiesAsync();
            if (props.Duration.TotalSeconds > MinimumFileDuration)
            {
                MusicFileModel model = new(props)
                {
                    FileName = file.Name,
                    Path = file.Path,
                    MusicFile = file
                };
                if (ShowThumbnails)
                {
                    try
                    {
                        using StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, ThumbnailSize);
                        BitmapImage bitmapImage = new();
                        await bitmapImage.SetSourceAsync(thumbnail);
                        model.Thumbnail = bitmapImage;
                    }
                    catch (Exception ex)
                    {
                        App.TraceError(ex);
                    }
                }

                return new MusicFileView(model)
                {
                    Comment = "🐢🤙"
                };
            }
            return null;
        }
        #endregion

        #region ui events
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MusicListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }

        private void LoadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            optionsPanel.Visibility = Visibility.Collapsed;
            musicListView.Visibility = Visibility.Visible;
            FileLoadingProgress.Visibility = Visibility.Visible;
            FileLoadingProgress.IsIndeterminate = true;

            var timer = DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.IsRepeating = false;
            timer.Tick += (s, e) => _ = LoadFiles();
            timer.Start();
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            IList<object> items = musicListView.SelectedItems;
            List<MusicFileModel> files = new();
            foreach (object item in items)
            {
                if (item is MusicFileView view)
                {
                    files.Add(view.Model);
                }
            }
            App.MainWindow.NavigateTo(typeof(MusicPlayerPage), files);
        }
        #endregion
    }
}
