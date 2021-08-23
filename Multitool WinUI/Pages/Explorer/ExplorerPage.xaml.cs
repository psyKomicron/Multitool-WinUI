using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Multitool.FileSystem;

using MultitoolWinUI.Models;

using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Multitool.FileSystem.Completion;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Multitool.Parsers;
using Multitool.Sorting;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Multitool.FileSystem.Events;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Explorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorerPage : Page
    {
        private static SolidColorBrush RED = new(Colors.Red);
        private static SolidColorBrush WHITE = new(Colors.White);

        // tri-state: [ignored, home, explorer, editor]
        private byte loaded;
        private CancellationTokenSource fsCancellationTokenSource;
        private IPathCompletor pathCompletor;
        private FileSystemManager fileSystemManager = new();
        private Stopwatch eventStopwatch = new();
        private Stopwatch taskStopwatch = new();
        private Stack<string> previousStackPath = new(10);
        private Stack<string> nextPathStack = new(10);
        private UriCleaner cleaner = new();

        public ExplorerPage()
        {
            InitializeComponent();
            ViewModel.CurrentDispatcherQueue = DispatcherQueue;
            fileSystemManager.Progress += FileSystemManager_Progress;
            fileSystemManager.Exception += FileSystemManager_Exception;
            fileSystemManager.Completed += FileSystemManager_Completed;
            fileSystemManager.Change += FileSystemManager_Change;
        }

        #region properties

        public ObservableCollection<FileSystemEntryViewModel> CurrentFiles { get; } = new();

        public ObservableCollection<string> PathAutoCompletion { get; } = new();

        public ObservableCollection<string> History { get; } = new();

        public string CurrentPath { get; set; }

        #endregion

        #region private methods

        private void DisplayFiles(string path)
        {
            loaded |= 0b10;

            if (fsCancellationTokenSource != null)
            {
                fsCancellationTokenSource.Cancel();
                fsCancellationTokenSource.Dispose();
            }
            fsCancellationTokenSource = new CancellationTokenSource();

            try
            {
                string realPath = fileSystemManager.GetRealPath(UriCleaner.RemoveChariotReturns(path));
                CurrentPath = realPath;
#if false
                if (realPath.Length < 10 && !Data.History.Contains(realPath))
                {
                    Data.History.Add(realPath);
                }
                else if (realPath.Length >= 10 && !Data.History.Contains(string.Format("{0, 10}", realPath)))
                {
                    Data.History.Add(string.Format("{0, 10}", realPath));
                }
#endif
                PathAutoCompletion.Clear();
                CurrentFiles.Clear();
                Progress_TextBox.Text = string.Empty;
                fileSystemManager.Notify = Files_ProgressBar.IsIndeterminate = CancelAction_Button.IsEnabled = true;

                IList<FileSystemEntryViewModel> pathItems = CurrentFiles;
                eventStopwatch.Restart();
                taskStopwatch.Restart();
                try
                {
                    fileSystemManager.GetFileSystemEntries(realPath, CurrentFiles, AddDelegate, fsCancellationTokenSource.Token);
                    Trace.WriteLine(nameof(ExplorerPage) + " -> Waiting for filesystem manager to complete...");
                }
                catch (ArgumentException argExcep)
                {
                    eventStopwatch.Reset();
                    CancelAction_Button.IsEnabled = false;
                    Files_ProgressBar.IsIndeterminate = false;

                    Trace.WriteLine(argExcep);
                    Progress_TextBox.Text = argExcep.ToString();
                }
            }
            catch (DirectoryNotFoundException)
            {
                eventStopwatch.Reset();
                CancelAction_Button.IsEnabled = false;
                Files_ProgressBar.IsIndeterminate = false;

                CurrentPath = path;
                path = path.ToLowerInvariant();
#if false
                // remove from history
                for (int i = 0; i < Data.History.Count; i++)
                {
                    if (Data.History[i].Equals(path, StringComparison.OrdinalIgnoreCase))
                    {
                        Data.History.RemoveAt(i);
                        break;
                    }
                }
#endif
                Progress_TextBox.Text = "Directory not found (" + path + ").";
            }
        }

        private void AddDelegate(IList<FileSystemEntryViewModel> items, IFileSystemEntry item)
        {
            DispatcherQueue.TryEnqueue(() => items.Add(new FileSystemEntryViewModel(item)));
            //SortList();
        }

        private void SortList()
        {
            FileSystemEntryViewModel[] pathItems = ObservableCollectionQuickSort.Sort(CurrentFiles);
            CurrentFiles.Clear();
            for (int i = 0; i < pathItems.Length; i++)
            {
                CurrentFiles.Add(pathItems[i]);
            }
        }

        private void DisplayMessage(string message, bool error = false, bool force = false)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
              {
                  if (force || eventStopwatch.ElapsedMilliseconds > 30) //ms interval between each notification
                  {
                      if (error)
                      {
                          Progress_TextBox.Foreground = RED;
                          Progress_TextBox.Text = message;
                      }
                      else
                      {
                          Progress_TextBox.Foreground = WHITE;
                          Progress_TextBox.Text = message;
                      }
                      eventStopwatch.Restart();
                  }
              });
        }

        #endregion

        #region events

        #region navigation events
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string path = e.Parameter as string;
            if (!string.IsNullOrEmpty(path))
            {
                DisplayFiles(path);
            }
            base.OnNavigatedTo(e);
        }
        #endregion

        #region window events

        private void MainListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void RefreshFileList_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PathInput_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (fsCancellationTokenSource != null)
            {
                fsCancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region manager
        private void FileSystemManager_Progress(object sender, string message)
        {
            DisplayMessage(message, false, sender == null);
        }

        private void FileSystemManager_Exception(object sender, Exception exception)
        {
            DisplayMessage(exception.Message, true);
        }

        private void FileSystemManager_Change(object sender, FileChangeEventArgs data)
        {
            switch (data.ChangeTypes)
            {
                case WatcherChangeTypes.Created:
                    AddDelegate(CurrentFiles, data.Entry);
                    break;
                case WatcherChangeTypes.Deleted:
                    for (int i = 0; i < CurrentFiles.Count; i++)
                    {
                        if (CurrentFiles[i].FileSystemEntry.Equals(data.Entry))
                        {
                            CurrentFiles.RemoveAt(i);
                            return;
                        }
                    }
                    break;
#if TRACE
                case WatcherChangeTypes.Changed:
                    Trace.WriteLine(data.Entry.Path + " changed");
                    break;
                case WatcherChangeTypes.Renamed:
                    Trace.WriteLine(data.Entry.Path + " renamed");
                    break;
                case WatcherChangeTypes.All:
                    Trace.WriteLine(data.Entry.Path + " : all changes");
                    break;
#endif
            }
            data.InUse = false;
        }

        private void FileSystemManager_Completed(TaskStatus status, Task task)
        {
            taskStopwatch.Stop();
            if (fsCancellationTokenSource != null)
            {
                fsCancellationTokenSource.Dispose();
            }
            fsCancellationTokenSource = null;
            eventStopwatch.Reset();

            _ = DispatcherQueue.TryEnqueue(() =>
              {
                  CancelAction_Button.IsEnabled = false;
                  SortList();
                  Files_ProgressBar.IsIndeterminate = false;

                  string message = string.Empty;
                  switch (status)
                  {
                      case TaskStatus.RanToCompletion:
                          Progress_TextBox.Foreground = new SolidColorBrush(Colors.White);
                          message = "Task successfully completed";
                          break;
                      case TaskStatus.Canceled:
                          Progress_TextBox.Foreground = new SolidColorBrush(Colors.Orange);
                          message = "Task cancelled";
                          break;
                      case TaskStatus.Faulted:
                          Progress_TextBox.Foreground = new SolidColorBrush(Colors.Red);
                          message = "Task failed";
                          Trace.WriteLine(task.Exception.ToString());
                          break;
                  }

                  Progress_TextBox.Text = taskStopwatch.Elapsed.TotalSeconds >= 1
                      ? message + " (in " + taskStopwatch.Elapsed.TotalSeconds.ToString() + "s)"
                      : message + " (in " + taskStopwatch.ElapsedMilliseconds.ToString() + "ms)";
              });
        }

        #endregion

        #endregion

    }
}
