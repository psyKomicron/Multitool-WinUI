using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL.Settings;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;

using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPlayerPage : Page, INotifyPropertyChanged, IDisposable
    {
        private static readonly Regex audioMimeRegex = new(@"^audio/.+");
        private readonly MediaPlayer player = new()
        {
            AudioCategory = MediaPlayerAudioCategory.Media
        };
        private DispatcherQueueTimer progressTimer;
        private readonly DelayedActionQueue delayedActionQueue = new();
        private bool playing;
        private TimeSpan progress;

        public MusicPlayerPage()
        {
            InitializeComponent();
            delayedActionQueue.QueueEmpty += DelayedActionQueue_QueueEmpty;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        [Setting(null)]
        public string LastUsedPath { get; set; }

        [Setting(null)]
        public string LastPlayed { get; set; }

        [Setting(10)]
        public double Volume { get; set; }

        [Setting(true)]
        public bool ShowFolders { get; set; }

        public double Progress { get; set; } = 100;

        public MusicFileModel CurrentPlaying { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            player.Dispose();
        }

        #region navigation
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            #region settings
            App.Settings.Load(this);
            player.Volume = Volume / 100d;
            if (!string.IsNullOrEmpty(LastPlayed))
            {
                _ = LoadLastPlayed(LastPlayed);
            }

            PropertyChanged?.Invoke(this, new(string.Empty));
            #endregion

            #region navigation
            if (e.Parameter is List<string> pathes)
            {
                FileLoadingProgress.Visibility = Visibility.Visible;
                FileLoadingProgress.IsIndeterminate = true;
                _ = LoadFiles(pathes);
            }
            else if (e.Parameter is List<MusicFileModel> views)
            {
                MusicListView.Items.Clear();
                MusicListViewHeader.Text = "From search";
                foreach (MusicFileModel view in views)
                {
                    _ = CreateAddFile(view.MusicFile);
                }
                FileLoadingProgress.IsIndeterminate = false;
                FileLoadingProgress.Visibility = Visibility.Collapsed;
            }
            else
            {
                _ = ListFolder(LastUsedPath);
            } 
            #endregion
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.Settings.Save(this);
            MusicListView.Items.Clear();
            base.OnNavigatedFrom(e);
        }
        #endregion

        #region private methods
        private void DisplayMessage(string message)
        {
            //ErrorInfoBar.Title = message;
            InfoTextBlock.Text = message;
            ErrorInfoBar.IsOpen = true;
        }

        private void PauseOrResume()
        {
            if (playing)
            {
                player.Pause();
                progressTimer.Stop();
            }
            else
            {
                player.Play();
                progressTimer.Start();
            }
            playing = !playing;
        }

        private async Task LoadLastPlayed(string path)
        {
            if (File.Exists(path))
            {
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(path);
                MusicProperties properties = await storageFile.Properties.GetMusicPropertiesAsync();
                MusicFileModel model = new(properties)
                {
                    FileName = storageFile.Name,
                    Path = storageFile.Path,
                    PlayCount = 0
                };

                try
                {
                    StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 30);
                    BitmapImage image = new();
                    await image.SetSourceAsync(thumbnail);
                    model.Thumbnail = image;
                }
                catch (Exception ex)
                {
                    App.TraceError(ex);
                }
                await Play(model, false);
            }
        }

        private async Task Play(MusicFileModel model, bool play = true)
        {
            try
            {
                StorageFile file = model.MusicFile ?? await StorageFile.GetFileFromPathAsync(model.Path);
                using IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
                player.SetStreamSource(stream);
                if (play)
                {
                    player.Play();
                    playing = true;
                    delayedActionQueue.QueueAction(() => DisplayMessage($"Playing {model.Title}"));

                    progress = TimeSpan.Zero;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        MusicProgressBar.Value = 0;
                        MusicProgressBar.Maximum = model.AudioLength.TotalSeconds;
                    });
                    progressTimer.Start(); 
                }

                LastPlayed = model.Path;
                CurrentPlaying = model;
                model.PlayCount++;

                CurrentThumbnail.Source = model.Thumbnail;
                CurrentPlayingTitle.Text = model.Title;
                CurrentPlayingAlbum.Text = model.Album;
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }

        private async Task CreateAddFile(StorageFile storageFile)
        {
            if (storageFile.IsAvailable && audioMimeRegex.IsMatch(storageFile.ContentType))
            {
                MusicProperties properties = await storageFile.Properties.GetMusicPropertiesAsync();
                MusicFileModel model = new(properties)
                {
                    FileName = storageFile.Name,
                    Path = storageFile.Path,
                    PlayCount = 0
                };

                try
                {
                    StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 30);
                    BitmapImage image = new();
                    await image.SetSourceAsync(thumbnail);
                    model.Thumbnail = image;
                }
                catch (Exception ex)
                {
                    App.TraceError(ex);
                }
                MusicListView.DispatcherQueue.TryEnqueue(() => MusicListView.Items.Add(new MusicFileView(model)));
            }
        }

        private async Task ListFolder(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (Directory.Exists(path))
                {
                    MusicListView.Items.Clear();
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        MusicListViewHeader.Text = folder.Name;
                    });

                    IReadOnlyList<StorageFolder> storageFolders = await folder.GetFoldersAsync();
                    MusicListView.DispatcherQueue.TryEnqueue(() =>
                    {
                        for (int i = 0; i < storageFolders.Count; i++)
                        {
                            MusicListView.Items.Add(new TextBlock()
                            {
                                Text = storageFolders[i].Path
                            });
                        }
                    });

                    IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                    for (int i = 0; i < files.Count; i++)
                    {
                        await CreateAddFile(files[i]);
                    }
                }
                else
                {
                    App.TraceWarning($"Directory not found {path}");
                }
            }
        }

        private async Task LoadFiles(List<string> pathes)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                FileLoadingProgress.Minimum = 0;
                FileLoadingProgress.Maximum = pathes.Count;
                FileLoadingProgress.IsIndeterminate = false;
            });
            for (int i = 0; i < pathes.Count; i++)
            {
                DispatcherQueue.TryEnqueue(() => FileLoadingProgress.Value++);
                if (File.Exists(pathes[i]))
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(pathes[i]);
                    await CreateAddFile(file);
                }
                else
                {
                    delayedActionQueue.QueueAction(() => DisplayMessage($"{Path.GetFileName(pathes[i])} no found"));
                }
            }
        }
        #endregion

        #region events
        private void DelayedActionQueue_QueueEmpty(DelayedActionQueue sender, System.Timers.ElapsedEventArgs args)
        {
            ErrorInfoBar.IsOpen = false;
        }

        private void ProgressTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            progress = progress.Add(TimeSpan.FromSeconds(1));
            MusicProgressBar.Value = progress.TotalSeconds;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            delayedActionQueue.DispatcherQueue = DispatcherQueue;

            progressTimer = DispatcherQueue.CreateTimer();
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Interval = TimeSpan.FromSeconds(1);
        }

        #region ui events
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            App.Settings.Save(this);
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ListFolder(args.QueryText);
        }

        private async void MusicListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MusicListView.SelectedItem is MusicFileView view)
            {
                await Play(view.Model);
            }
            else if (MusicListView.SelectedItem is TextBlock block)
            {
                await ListFolder(block.Text);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.Volume = e.NewValue / 100d;
        }

        private void SearchMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (!App.MainWindow.ContentFrame.Navigate(typeof(MusicSearchPage)))
            {
                App.TraceWarning("Ooops. Failed to navigate to the search page, try again.");
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ListFolder(LastUsedPath);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PauseOrResume();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Space)
            {
                PauseOrResume();
                e.Handled = true;
            }
        }
        #endregion
        #endregion
    }
}
