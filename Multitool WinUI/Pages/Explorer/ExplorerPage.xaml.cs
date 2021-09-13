using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.FileSystem;
using Multitool.FileSystem.Completion;
using Multitool.FileSystem.Events;
using Multitool.Parsers;
using Multitool.Sorting;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private IPathCompletor pathCompletor = new PathCompletor();
        private FileSystemManager fileSystemManager = new() { Notify = false };
        private Stopwatch eventStopwatch = new();
        private Stopwatch taskStopwatch = new();
        private Stack<string> previousStackPath = new(10);
        private Stack<string> nextPathStack = new(10);
        //private UriCleaner cleaner = new();

        public ExplorerPage()
        {
            InitializeComponent();
            fileSystemManager.Progress += FileSystemManager_Progress;
            fileSystemManager.Exception += FileSystemManager_Exception;
            fileSystemManager.Completed += FileSystemManager_Completed;
            fileSystemManager.Change += FileSystemManager_Change;
        }

        #region properties

        public ObservableCollection<FileSystemEntryViewModel> CurrentFiles { get; } = new();

        public ObservableCollection<string> History { get; } = new();

        public string CurrentPath { get; set; }

        #endregion

        #region private methods

        private async void DisplayFiles(string path)
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
                CurrentFiles.Clear();
                Progress_TextBox.Text = string.Empty;
                fileSystemManager.Notify = Files_ProgressBar.IsIndeterminate = CancelAction_Button.IsEnabled = true;

                IList<FileSystemEntryViewModel> pathItems = CurrentFiles;
                eventStopwatch.Restart();
                taskStopwatch.Restart();
                try
                {
                    await fileSystemManager.GetFileSystemEntries(realPath, CurrentFiles, AddDelegate, fsCancellationTokenSource.Token);

                    taskStopwatch.Stop();
                    if (fsCancellationTokenSource != null)
                    {
                        fsCancellationTokenSource.Dispose();
                    }
                    fsCancellationTokenSource = null;
                    eventStopwatch.Reset();

                    CancelAction_Button.IsEnabled = false;
                    SortList();
                    Files_ProgressBar.IsIndeterminate = false;

                    Progress_TextBox.Foreground = new SolidColorBrush(Colors.White);
                    string message = "Task successfully completed";

                    Progress_TextBox.Text = taskStopwatch.Elapsed.TotalSeconds >= 1
                        ? message + " (in " + taskStopwatch.Elapsed.TotalSeconds.ToString() + "s)"
                        : message + " (in " + taskStopwatch.ElapsedMilliseconds.ToString() + "ms)";
                }
                catch (Exception ex) // we catch everything, and display it to the trace and UI
                {
                    eventStopwatch.Reset();
                    CancelAction_Button.IsEnabled = false;
                    Files_ProgressBar.IsIndeterminate = false;

                    Trace.WriteLine(ex.ToString());
                    Progress_TextBox.Text = ex.ToString();
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
            _ = DispatcherQueue.TryEnqueue(() => items.Add(new FileSystemEntryViewModel(item, DispatcherQueue)));
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

        private void Next()
        {
            if (nextPathStack.Count > 0)
            {
                previousStackPath.Push(CurrentPath);
                DisplayFiles(nextPathStack.Pop());
            }
        }

        private void Back()
        {
            if (previousStackPath.Count > 0)
            {
                nextPathStack.Push(CurrentPath);
                DisplayFiles(previousStackPath.Pop());
            }
            else
            {
                DirectoryInfo info = Directory.GetParent(CurrentPath);
                if (info != null)
                {
                    nextPathStack.Push(CurrentPath);
                    DisplayFiles(info.FullName);
                }
            }
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

        private void MainListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MainListView.SelectedItem is FileSystemEntryViewModel item)
            {
                DisplayFiles(item.Path);
            }
            e.Handled = true;
        }

        private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsXButton1Pressed)
            {
                Back();
            }
            else if (e.GetCurrentPoint(null).Properties.IsXButton2Pressed)
            {
                Next();
            }
        }

        private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // TODO
        }

        private void RefreshFileList_Click(object sender, RoutedEventArgs e)
        {
            DisplayFiles(CurrentPath);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (fsCancellationTokenSource != null)
            {
                fsCancellationTokenSource.Cancel();
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void PathInput_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string path = PathInput.Text;
            try
            {
                PathInput.ItemsSource = pathCompletor.Complete(path);
            }
            catch (UnauthorizedAccessException) { }
        }

        private void PathInput_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
#if DEBUG
            Debug.WriteLine("PathInput_SuggestionChosen -> " + args.SelectedItem.ToString() + " | " + args.GetType().ToString());
#endif
        }

        private void PathInput_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string text = args.QueryText;
            if (!string.IsNullOrEmpty(text))
            {
                DisplayFiles(text);
            }
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
