using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL;
using Multitool.DAL.FileSystem;
using Multitool.DAL.Settings;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;


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
        private readonly FileSearcher searcher = new(ignoreList)
        {
            ThreadCount = 4
        };

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
        [Setting(true)]
        public bool CacheFiles { get; set; }
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
            FileLoadingProgress.IsIndeterminate = true;
            FileLoadingProgress.Visibility = Visibility.Visible;

            if (!File.Exists(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "music files.tmp")))
            {
                var files = await searcher.SearchForType(FileType.Audio);

                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    foreach (var file in files)
                    {
                        MusicFileView view = await CreateView(file);
                        if (view != null)
                        {
                            musicListView.Items.Add(view);
                        }
                    }
                });

                if (CacheFiles)
                {
                    StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("music files.tmp");
                    using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                    using var outputStream = stream.GetOutputStreamAt(0);
                    using DataWriter writer = new(stream);
                    foreach (var fileName in files)
                    {
                        writer.WriteBuffer(CryptographicBuffer.ConvertStringToBinary(fileName + "\n", BinaryStringEncoding.Utf8));
                    }
                    await writer.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            else
            {
                StorageFile cache = await ApplicationData.Current.TemporaryFolder.GetFileAsync("music files.tmp");
                using var stream = await cache.OpenAsync(FileAccessMode.Read);
                using var outputStream = stream.GetInputStreamAt(0);
                using DataReader reader = new(stream);
                Memory<char> data = new (reader.ReadString(await reader.LoadAsync((uint)stream.Size)).ToCharArray());
                List<string> filePathes = new();
                int previousIndex = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data.Span[i] == '\n')
                    {
                        filePathes.Add(data[previousIndex..i].ToString());
                        i++;
                        previousIndex = i;
                    }
                }
                DispatcherQueue.TryEnqueue(async () =>
                {
                    foreach (string filePath in filePathes)
                    {
                        MusicFileView view = await CreateView(filePath);
                        if (view != null)
                        {
                            musicListView.Items.Add(view);
                        }
                    }
                });
            }

            DispatcherQueue?.TryEnqueue(() =>
            {
                FileLoadingProgress.IsIndeterminate = false;
                FileLoadingProgress.Visibility = Visibility.Collapsed;
            });
        }

        private async Task<MusicFileView> CreateView(string fullPath)
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

            _ = LoadFiles();
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
