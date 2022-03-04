using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data.Settings;

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

namespace MultitoolWinUI.Pages.Test
{
    public sealed partial class SpotlightImporter : UserControl, INotifyPropertyChanged
    {
        private static bool handlerAttached;
        private readonly FlipView flipView;
        private readonly ProgressRing progressRing;
        private readonly PipsPager pipsPager;

        public SpotlightImporter()
        {
            InitializeComponent();
            flipView = (FlipView)FindName("ImageFlipView");
            progressRing = (ProgressRing)FindName("ImportProgress");
            pipsPager = (PipsPager)FindName("PipsPager");

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

        [Setting(true)]
        public bool OpenTempFolder { get; set; } 
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task ImportFiles()
        {
            try
            {
                progressRing.Visibility = Visibility.Visible;
                progressRing.IsIndeterminate = true;
                List<SpotlightItem> files = new();
                foreach (var item in Items)
                {
                    if (item.IsSelected)
                    {
                        files.Add(item);
                    }
                }

                // show FilePicker dialog
                IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                if (hwnd != IntPtr.Zero)
                {
                    FolderPicker picker = new();
                    InitializeWithWindow.Initialize(picker, hwnd);
                    picker.ViewMode = PickerViewMode.Thumbnail;
                    picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                    picker.FileTypeFilter.Add("*");
                    // I18N
                    picker.CommitButtonText = "Select folder to import to.";

                    StorageFolder folder = await picker.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        progressRing.IsIndeterminate = false;
                        progressRing.Value = 0;
                        progressRing.Maximum = files.Count;
                        progressRing.Minimum = 0;
                        // import files
                        for (int i = 0; i < files.Count; i++)
                        {
                            string newPath = Path.Combine(folder.Path, files[i].FileName);
                            File.Move(files[i].Path, newPath);

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                progressRing.Value++;
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Cannot import Spotlight files right now.");
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressRing.IsEnabled = false;
                    progressRing.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async Task LoadFiles()
        {
            try
            {
                progressRing.Visibility = Visibility.Visible;
                progressRing.IsIndeterminate = true;
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
                progressRing.IsIndeterminate = false;
                progressRing.Minimum = 0;
                progressRing.Maximum = files.Length;
                progressRing.Value = 0;
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
                        Trace.TraceInformation($"{newPath} aldready exists, not overwriting.");
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        progressRing.Value++;
                    });
                }

                // show files
                DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < newFiles.Count; i++)
                    {
                        Items.Add(new SpotlightItem(newFiles[i], Path.Combine(folder.Path, newFiles[i])));
                        progressRing.Value++;
                        pipsPager.NumberOfPages++;
                    }
                    progressRing.IsEnabled = false;
                    progressRing.Visibility = Visibility.Collapsed;
                });

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
            if (flipView.SelectedItem is SpotlightItem item)
            {
                item.IsSelected = !item.IsSelected;
                PropertyChanged?.Invoke(this, new(nameof(ImportButtonEnabled)));
            }
        }

        private void ClearTempDataButton_Click(object sender, RoutedEventArgs e)
        {
            pipsPager.NumberOfPages = 0;
            PropertyChanged?.Invoke(this, new(nameof(ImportButtonEnabled)));
            Items.Clear();
            _ = ClearTempData();
        }
        #endregion

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
