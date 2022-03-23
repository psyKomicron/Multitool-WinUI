using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data.Settings;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class SpotlightImporter : UserControl, INotifyPropertyChanged
    {
        private static bool handlerAttached;

        public SpotlightImporter()
        {
            InitializeComponent();

            if (!handlerAttached)
            {
                App.MainWindow.Closed += MainWindow_Closed;
                handlerAttached = true;
            }
            try
            {
                App.UserSettings.Load(this);
            }
            catch { }
        }

        #region properties
        public bool ClearOnUnloaded { get; set; } = false;

        [Setting(CreationCollisionOption.ReplaceExisting)]
        public CreationCollisionOption CollisionOption { get; set; }

        [Setting(true)]
        public bool DeleteTempData { get; set; }

        public bool ImportButtonEnabled
        {
            get
            {
                foreach (var item in Items)
                {
                    if (item.IsSelected)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public ObservableCollection<SpotlightItem> Items { get; } = new();

        [Setting(CommandBarDefaultLabelPosition.Right)]
        public CommandBarDefaultLabelPosition MenuBarLabelPosition { get; set; }

        [Setting(false)]
        public bool OpenTempFolder { get; set; }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        #region private methods
        private async Task ImportFiles()
        {
            try
            {
                ImportProgress.Visibility = Visibility.Visible;
                ImportProgress.IsIndeterminate = true;
                List<SpotlightItem> files = new();
                foreach (var item in Items)
                {
                    if (item.IsSelected)
                    {
                        files.Add(item);
                    }
                }

                // show FilePicker dialog
                FolderPicker picker = Tool.CreateFolderPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add("*");
                // I18N
                picker.CommitButtonText = "Select folder to import to.";

                StorageFolder folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    ImportProgress.IsIndeterminate = false;
                    ImportProgress.Value = 0;
                    ImportProgress.Maximum = files.Count;
                    ImportProgress.Minimum = 0;
                    // import files
                    for (int i = 0; i < files.Count; i++)
                    {
                        string newPath = Path.Combine(folder.Path, files[i].FileName);
                        File.Move(files[i].Path, newPath);

                        DispatcherQueue.TryEnqueue((Microsoft.UI.Dispatching.DispatcherQueueHandler)(() =>
                        {
                            this.ImportProgress.Value++;
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Cannot import Spotlight files right now.");
            }
            finally
            {
                DispatcherQueue.TryEnqueue((Microsoft.UI.Dispatching.DispatcherQueueHandler)(() =>
                {
                    this.ImportProgress.IsEnabled = false;
                    this.ImportProgress.Visibility = Visibility.Collapsed;
                }));
            }
        }

        private async Task LoadFiles()
        {
            try
            {
                ImportProgress.Visibility = Visibility.Visible;
                ImportProgress.IsIndeterminate = true;
                var files = Directory.GetFiles(@$"{UserDataPaths.GetDefault().LocalAppData}\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");
                if (files.Length == 0)
                {
                    App.TraceInformation("Spotlight asset folder is empty, nothing to import from it.");
                    return;
                }

                List<string> newFiles = new();

                // import files
                Regex regex = new(@"\.[A-z0-9]+$");
                StorageFolder folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("spotlight", CreationCollisionOption.OpenIfExists);
                ImportProgress.IsIndeterminate = false;
                ImportProgress.Minimum = 0;
                ImportProgress.Maximum = files.Length;
                ImportProgress.Value = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);
                    if (!regex.IsMatch(fileName))
                    {
                        fileName += ".png";
                    }
                    newFiles.Add(fileName);

                    string newPath = Path.Combine(folder.Path, fileName);
                    if (!File.Exists(newPath))
                    {
                        File.Copy(files[i], newPath, false);
                        Trace.TraceInformation($"Copying {fileName} to {folder.Path}");
                    }
                    else
                    {
                        Trace.TraceInformation($"{fileName} aldready exists, not overwriting.");
                    }

                    DispatcherQueue.TryEnqueue((Microsoft.UI.Dispatching.DispatcherQueueHandler)(() =>
                    {
                        this.ImportProgress.Value++;
                    }));
                }

                // show files
                DispatcherQueue.TryEnqueue((Microsoft.UI.Dispatching.DispatcherQueueHandler)(() =>
                {
                    for (int i = 0; i < newFiles.Count; i++)
                    {
                        Items.Add(new SpotlightItem(newFiles[i], Path.Combine(folder.Path, newFiles[i])));
                        this.ImportProgress.Value++;
                        PipsPager.NumberOfPages++;
                    }
                    this.ImportProgress.IsEnabled = false;
                    this.ImportProgress.Visibility = Visibility.Collapsed;
                }));

                if (OpenTempFolder)
                {
                    await Launcher.LaunchFolderAsync(folder);
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Failed to load Spotlight files (from Spotlight assets folder).");
            }
        }

        private async Task ClearTempData()
        {
            if (DeleteTempData && Directory.Exists($"{ApplicationData.Current.TemporaryFolder.Path}\\spotlight"))
            {
                Trace.TraceInformation("Clearing temp data");
                try
                {
                    StorageFolder folder = await ApplicationData.Current.TemporaryFolder.GetFolderAsync("spotlight");
                    var files = await folder.GetFilesAsync();
                    List<Task> deletes = new(files.Count);
                    foreach (var file in files)
                    {
                        deletes.Add(file.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
                    }
                    await Task.WhenAll(deletes);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            else
            {
                if (DeleteTempData)
                {
                    Trace.TraceWarning("Not deleting temp folder, it does not exists");
                }
            }
        } 

        private void SelectImage()
        {
            if (ImageFlipView.SelectedItem is SpotlightItem item)
            {
                item.IsSelected = !item.IsSelected;
                PropertyChanged?.Invoke(this, new(nameof(ImportButtonEnabled)));
            }
        }
        #endregion

        #region event handlers
        private async void ImportAllButton_Click(object sender, RoutedEventArgs e) => await ImportFiles();

        private async void OpenSpotlightFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");
                if (!await Launcher.LaunchUriAsync(new(path)))
                {
                    App.TraceWarning("Failed to open Spotlight folder.");
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => _ = LoadFiles();

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ClearOnUnloaded)
            {
                _ = ClearTempData();
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args) => _ = ClearTempData();

        private void Image_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectImage();
            e.Handled = true;
        }

        private void ClearTempDataButton_Click(object sender, RoutedEventArgs e)
        {
            PipsPager.NumberOfPages = 0;
            PropertyChanged?.Invoke(this, new(nameof(ImportButtonEnabled)));
            Items.Clear();
            _ = ClearTempData();
        }

        //private void CheckBox_Checked(object sender, RoutedEventArgs e) => SelectImage();
        #endregion
    }
}
