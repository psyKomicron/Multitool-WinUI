using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;

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

        public ObservableCollection<string> CrumbPath { get; } = new();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string path)
            {
                string[] folders = path.Split(Path.DirectorySeparatorChar);
                foreach (string folder in folders)
                {
                    CrumbPath.Add(folder);
                }
                _ = LoadFolder(path);
            }
            else if (e.Parameter is List<MusicFileModel> views)
            {
                FileLoadingProgress.Visibility = Visibility.Visible;
                FilesListView.Items.Clear();
                foreach (MusicFileModel view in views)
                {
                    _ = CreateAddFile(view);
                }
                FileLoadingProgress.IsIndeterminate = false;
                FileLoadingProgress.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadFolder(string path)
        {
            try
            {
                FilesListView.DispatcherQueue.TryEnqueue(() => FilesListView.Items.Clear());

                StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
                IReadOnlyList<StorageFolder> folders = await rootFolder.GetFoldersAsync();
                foreach (StorageFolder folder in folders)
                {
                    MusicFileModel model = new()
                    {
                        MimeType = "folder",
                        Path = folder.Path
                    };
                    model.FileName = folder.Name;
                    FilesListView.DispatcherQueue.TryEnqueue(() => FilesListView.Items.Add(model));
                }

                IReadOnlyList<StorageFile> files = await rootFolder.GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    MusicFileModel model = new()
                    {
                        MimeType = file.ContentType,
                        Path = file.Path
                    };
                    model.FileName = file.Name;
                    FilesListView.DispatcherQueue.TryEnqueue(() => FilesListView.Items.Add(model));
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }

        private async Task CreateAddFile(MusicFileModel model)
        {
            if (model.MusicFile != null && model.MusicFile.IsAvailable)
            {
                try
                {
                    if (model.Thumbnail == null)
                    {
                        StorageItemThumbnail thumbnail = await model.MusicFile.GetThumbnailAsync(ThumbnailMode.MusicView, 90);
                        BitmapImage image = new();
                        await image.SetSourceAsync(thumbnail);
                        model.Thumbnail = image;
                    }
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
                items.Add(((MusicFileModel)item).Path);
            }
            App.MainWindow.NavigateTo(typeof(MusicPlayerPage), items);
        }

        private void PictureSelection_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }
}
