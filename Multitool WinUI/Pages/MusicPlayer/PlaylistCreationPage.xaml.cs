using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data.FileSystem;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playlists;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;

using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistCreationPage : Page
    {
        public PlaylistCreationPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                FileLoadingProgress.Visibility = Visibility.Visible;
                FileLoadingProgress.IsIndeterminate = true;
                if (e.Parameter is List<MusicFileModel> views)
                {
                    FilesListView.Items.Clear();
                    foreach (MusicFileModel view in views)
                    {
                        _ = CreateAddFile(view);
                    }
                }
                else
                {
                    StorageFolder folder = KnownFolders.MusicLibrary;
                    IAsyncOperation<IReadOnlyList<StorageFile>> musicFilesTask = folder.GetFilesAsync();
                    Regex regex = new(@"^audio/.*");
                    List<Task> tasks = new();
                    IReadOnlyList<StorageFile> musicFiles = await musicFilesTask;
                    foreach (StorageFile musicFile in musicFiles)
                    {
                        if (regex.IsMatch(musicFile.ContentType))
                        {
                            MusicFileModel model = new()
                            {
                                File = musicFile,
                                Name = musicFile.Name,
                                Path = musicFile.Path,
                            };
                            tasks.Add(CreateAddFile(model));
                        }
                    }
                    await Task.WhenAll(tasks);

                    FileSearcher searcher = new()
                    {
                        ThreadCount = 2
                    };
                    searcher.IgnoreList = new(@"c:\\|Steam|\$RECYCLE.BIN", RegexOptions.IgnoreCase);
                    List<string> files = await searcher.SearchForType(FileType.Audio);
                    foreach (string file in files)
                    {
                        var musicFile = await StorageFile.GetFileFromPathAsync(file);
                        MusicFileModel model = new()
                        {
                            File = musicFile,
                            Name = musicFile.Name,
                            Path = file
                        };
                        tasks.Add(CreateAddFile(model));
                    }
                    await Task.WhenAll(tasks);
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    FileLoadingProgress.IsIndeterminate = false;
                    FileLoadingProgress.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }

        private async Task CreateAddFile(MusicFileModel model)
        {
            if (model.File != null && model.File.IsAvailable)
            {
                try
                {
                    if (model.Thumbnail == null)
                    {
                        StorageItemThumbnail thumbnail = await model.File.GetThumbnailAsync(ThumbnailMode.MusicView, 90);
                        BitmapImage image = new();
                        await image.SetSourceAsync(thumbnail);
                        model.Thumbnail = image;
                    }
                    await model.SetMusicPropertiesAsync();
                }
                catch (Exception ex)
                {
                    App.TraceError(ex);
                }
                FilesListView.DispatcherQueue.TryEnqueue(() => FilesListView.Items.Add(new MusicFileView(model)));
            }
        }

        #region ui events
        private void FilesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MusicFileModel model)
            {
                model.Selected = !model.Selected;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                foreach (var item in FilesListView.Items)
                {
                    if (item is MusicFileModel model)
                    {
                        model.Selected = true;
                    }
                }
            }
            else
            {
                foreach (var item in FilesListView.Items)
                {
                    if (item is MusicFileModel model)
                    {
                        model.Selected = false;
                    }
                }
            }
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> items = new();
            foreach (var item in FilesListView.Items)
            {
                items.Add(((MusicFileView)item).Path);
            }
            //Playlist
            App.MainWindow.NavigateTo(typeof(MusicPlayerPage), items);
        }

        private async void PictureSelection_Click(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindow);

            if (hwnd != IntPtr.Zero)
            {
                FileOpenPicker picker = new();
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add("*");

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            PlaylistThumbnail.Source = new BitmapImage()
                            {
                                UriSource = new(file.Path)
                            };
                        }
                        catch (Exception ex)
                        {
                            App.TraceError(ex);
                        }
                    });
                } 
            }
        }
        #endregion
    }
}
