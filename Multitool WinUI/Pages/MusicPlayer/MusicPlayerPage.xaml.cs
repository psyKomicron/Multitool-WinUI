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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly DelayedActionQueue delayedActionQueue = new();
        private readonly DispatcherQueueTimer progressTimer;
        private bool loaded;
        private bool playing;

        public MusicPlayerPage()
        {
            InitializeComponent();
            try
            {
                App.Settings.Load(this);
                player.Volume = Volume / 100d;
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }

            App.MainWindow.Closed += MainWindow_Closed;

            player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            player.PlaybackSession.BufferingEnded += PlaybackSession_BufferingEnded;
            player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged; ;

            delayedActionQueue.DispatcherQueue = DispatcherQueue;
            delayedActionQueue.QueueEmpty += DelayedActionQueue_QueueEmpty;

            progressTimer = DispatcherQueue.CreateTimer();
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Interval = TimeSpan.FromMilliseconds(500);
        }

        #region Properties
        [Setting(null)]
        public string LastUsedPath { get; set; }

        [Setting(null)]
        public string LastPlayed { get; set; }

        [Setting(10)]
        public double Volume { get; set; }

        [Setting(true)]
        public bool ShowFolders { get; set; }

        [Setting(typeof(PlaylistModelSettingConverter), DefaultInstanciate = true)]
        public ObservableCollection<PlaylistModel> Playlists { get; set; }

        [Setting]
        public List<string> Files { get; set; }

        public double Progress { get; set; } = 100;

        public MusicFileModel CurrentPlaying { get; set; } 
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            player.Dispose();
        }

        #region navigation
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!loaded)
            {
                if (!string.IsNullOrEmpty(LastPlayed))
                {
                    _ = LoadLastPlayed(LastPlayed);
                }
            }

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
                FileLoadingProgress.IsIndeterminate = true;
                FileLoadingProgress.Visibility = Visibility.Visible;
                foreach (MusicFileModel view in views)
                {
                    _ = CreateAddFile(view.MusicFile);
                }
                FileLoadingProgress.IsIndeterminate = false;
                FileLoadingProgress.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (e.Parameter != null)
                {
                    DisplayMessage("Something went wrong...");
                    App.TraceWarning($"Navigation info not recognized {e.Parameter.GetType().Name}");
                }
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
            delayedActionQueue.QueueAction(() =>
            {
                InfoTextBlock.Text = message;
                ErrorInfoBar.IsOpen = true;
            });
        }

        private void SetPaused()
        {
            progressTimer.Stop();
        }

        private void SetPlaying()
        {
            progressTimer.Start();
        }

        private void AutoComplete()
        {
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                List<string> names = new();
                Regex search = new(Regex.Escape(SearchBox.Text), RegexOptions.IgnoreCase);
                foreach (var item in MusicListView.Items)
                {
                    if (item is MusicFileView view && search.IsMatch(view.Title))
                    {
                        names.Add(view.Title);
                    }
                }
                SearchBox.ItemsSource = names;
            }
            else
            {
                SearchBox.ItemsSource = null;
            }
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
                    StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 90);
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

                DispatcherQueue.TryEnqueue(() =>
                {
                    MusicProgressBar.Value = 0;
                    MusicProgressBar.Maximum = model.AudioLength.TotalSeconds;
                });

                if (play)
                {
                    player.Play();
                    model.PlayCount++;
                }

                LastPlayed = model.Path;
                CurrentPlaying = model;
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
                    StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 90);
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
                    DisplayMessage($"{Path.GetFileName(pathes[i])} no found");
                }
            }
        }
        #endregion

        #region events
        #region page events
        private void DelayedActionQueue_QueueEmpty(DelayedActionQueue sender, System.Timers.ElapsedEventArgs args)
        {
            ErrorInfoBar.IsOpen = false;
        }

        private void ProgressTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            MusicProgressBar.Value = player.PlaybackSession.Position.TotalSeconds;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
        } 
        #endregion

        #region player events
        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                playing = false;
                SetPaused();
            }
            else if (!playing && player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                playing = true;
                SetPlaying();
            }
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            //Debug.WriteLine($"Position changed {player.PlaybackSession.Position:T}");
            if (player.PlaybackSession.Position == CurrentPlaying.AudioLength)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < MusicListView.Items.Count; i++)
                    {
                        object item = MusicListView.Items[i];
                        if (item is MusicFileView view && view.Model == CurrentPlaying)
                        {
                            if (i + 1 < MusicListView.Items.Count)
                            {
                                _ = Play((MusicListView.Items[i + 1] as MusicFileView).Model);
                            }
                        }
                    }
                });
            }
        }

        private void PlaybackSession_BufferingEnded(MediaPlaybackSession sender, object args)
        {
            Debug.WriteLine("Buffering ended");
        } 
        #endregion

        #region ui events
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            App.Settings.Save(this);
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                for (int i = 0; i < MusicListView.Items.Count; i++)
                {
                    if (MusicListView.Items[i] is MusicFileView view)
                    {
                        await Play(view.Model);
                        return;
                    }
                }
                DisplayMessage($"Cannot play {args.ChosenSuggestion}");
            }
            else
            {
                await ListFolder(args.QueryText);
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            AutoComplete();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AutoComplete();
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
            if (!App.MainWindow.NavigateTo(typeof(MusicSearchPage)))
            {
                App.TraceWarning("Ooops. Failed to navigate to the search page, try again.");
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (!App.MainWindow.NavigateTo(typeof(PlaylistCreationPage)))
            {
                App.TraceWarning("Ooops. Failed to go to the playlist creation page, try again.");
            }
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
            if (player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                player.Pause();
            }
            else if (player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                player.Play();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Space)
            {
                player.Pause();
                e.Handled = true;
            }
        }
        #endregion
        #endregion
    }
}
