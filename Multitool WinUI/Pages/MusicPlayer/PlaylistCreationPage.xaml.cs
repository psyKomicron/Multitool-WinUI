using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
        }

        private void SwitchToEdit()
        {
            PathInputBox.Text = Path.GetFullPath(Path.Combine(CrumbPath.ToArray()));
            PathBar.Visibility = Visibility.Collapsed;
            PathInputBox.Visibility = Visibility.Visible;
        }

        private void SwitchToBreadcrumb()
        {
            PathBar.Visibility = Visibility.Visible;
            PathInputBox.Visibility = Visibility.Collapsed;
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

        #region ui events
        private void PathBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            int index = CrumbPath.IndexOf(args.Item.ToString());
            for (int i = CrumbPath.Count - 1; i > index; i--)
            {
                CrumbPath.RemoveAt(i);
            }
            _ = LoadFolder(Path.GetFullPath(Path.Combine(CrumbPath.ToArray())));
        }

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

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            _ = LoadFolder(args.QueryText);

            CrumbPath.Clear();
            string path = Path.GetFullPath(args.QueryText);
            string[] folders = path.Split(Path.DirectorySeparatorChar);
            foreach (string folder in folders)
            {
                CrumbPath.Add(folder);
            }
            SwitchToBreadcrumb();
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> items = new();
            foreach (var item in FilesListView.Items)
            {
                items.Add(((MusicFileModel)item).Path);
            }
            App.MainWindow.ContentFrame.Navigate(typeof(MusicPlayerPage), items);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToEdit();
        }
        #endregion
    }
}
