using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL;
using Multitool.DAL.Completion;
using Multitool.DAL.FileSystem;
using Multitool.DAL.FileSystem.Events;
using Multitool.Parsers;
using Multitool.Collections.Sorting;
using Multitool.Threading;

using MultitoolWinUI.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;

using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Explorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorerPage : Page, INotifyPropertyChanged
    {
        private static readonly SolidColorBrush RED = new(Colors.Red);
        private static readonly SolidColorBrush WHITE = new(Colors.White);

        private readonly IPathCompletor pathCompletor = new PathCompletor();
        private readonly IFileSystemManager fileSystemManager = new FileSystemManager() { Notify = false };
        private readonly Stopwatch eventStopwatch = new();
        private readonly Stack<string> previousStackPath = new(10);
        private readonly Stack<string> nextPathStack = new(10);
        private readonly Stopwatch taskStopwatch = new();
        private readonly object sortingLock = new();
        private ListenableCancellationTokenSource fsCancellationTokenSource;
        private string _currentPath;

        public ExplorerPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += OnMainWindowClosed;
            fileSystemManager.Progress += FileSystemManager_Progress;
            fileSystemManager.Exception += FileSystemManager_Exception;
            fileSystemManager.Changed += FileSystemManager_Change;

            try
            {
                CurrentPath = App.Settings.GetSetting<string>(nameof(ExplorerPage), nameof(CurrentPath));
            }
            catch (SettingNotFoundException ex)
            {
                Trace.TraceError(ex.ToString());
            }

#if DEBUG
            Button button = new()
            {
                Content = "R"
            };
            button.Click += (object sender, RoutedEventArgs e) =>
            {
                FileSystemManager.RefreshAllCache();
            };
            ButtonsStackPanel.Children.Add(button);
#endif
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        public ObservableCollection<FileSystemEntryView> CurrentFiles { get; } = new();

        public ObservableCollection<string> History { get; } = new();

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentPath)));
            }
        }
        #endregion

        #region private methods
        private void DisplayFiles(string path)
        {
            if (fsCancellationTokenSource != null)
            {
                async void a(ListenableCancellationTokenSource sender, EventArgs args)
                {
                    await LoadFiles(path);
                    fsCancellationTokenSource.Cancelled -= a;
                }
                fsCancellationTokenSource.Cancelled += a;
                CancelFileTask();
            }
            else
            {
                fsCancellationTokenSource = new();
                _ = LoadFiles(path);
            }
        }

        private async Task LoadFiles(string path)
        {
            try
            {
                string realPath = fileSystemManager.GetRealPath(UriCleaner.RemoveChariotReturns(path));
                CurrentPath = realPath;
                CurrentFiles.Clear();
                Progress_TextBox.Text = string.Empty;

                fileSystemManager.Notify = true;
                Files_ProgressBar.IsIndeterminate = true;
                CancelAction_Button.IsEnabled = true;

                eventStopwatch.Restart();
                taskStopwatch.Restart();
                try
                {
                    await fileSystemManager.GetFileSystemEntries(realPath, CurrentFiles, AddDelegate, fsCancellationTokenSource.Token);

                    taskStopwatch.Stop();
                    fsCancellationTokenSource.Dispose();
                    fsCancellationTokenSource = null;
                    eventStopwatch.Reset();

                    CancelAction_Button.IsEnabled = false;
                    SortList();
                    Files_ProgressBar.IsIndeterminate = false;

                    Progress_TextBox.Foreground = new SolidColorBrush(Colors.White);
                    if (taskStopwatch.Elapsed.TotalSeconds < 1)
                    {
                        Progress_TextBox.Text = "Task successfully completed (in " + taskStopwatch.ElapsedMilliseconds.ToString() + "ms)";
                    }
                    else
                    {
                        TimeSpan elapsed = taskStopwatch.Elapsed;
                        Progress_TextBox.Text = "Task successfully completed (in " + elapsed.ToString("mm\\:ss") + ")";
                    }

                    for (int i = 0; i < CurrentFiles.Count; i++)
                    {
                        FileSystemEntryView item = CurrentFiles[i];
                        if (item.IsDirectory && !Directory.Exists(item.Path))
                        {
                            _ = DispatcherQueue.TryEnqueue(() => item.Color = new SolidColorBrush(Colors.Red));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    eventStopwatch.Reset();
                    CancelAction_Button.IsEnabled = false;
                    Files_ProgressBar.IsIndeterminate = false;

                    fsCancellationTokenSource.InvokeCancel();
                    fsCancellationTokenSource = null;
                    Trace.TraceError("Operation cancelled, loading path : " + path);
                    Progress_TextBox.Text = "Operation cancelled";
                    DispatcherQueue.TryEnqueue(() => SortList());
                }
                catch (Exception ex) // we catch everything, and display it to the trace and UI
                {
                    eventStopwatch.Reset();
                    CancelAction_Button.IsEnabled = false;
                    Files_ProgressBar.IsIndeterminate = false;

                    Trace.TraceError(ex.ToString());
                    Progress_TextBox.Text = ex.ToString();
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                eventStopwatch.Reset();
                CancelAction_Button.IsEnabled = false;
                Files_ProgressBar.IsIndeterminate = false;

                Trace.TraceError(ex.ToString());
                CurrentPath = string.Empty;
                Progress_TextBox.Text = "Directory not found : '" + path + "'";
            }
        }

        private void AddDelegate(IList<FileSystemEntryView> items, IFileSystemEntry item)
        {
            _ = DispatcherQueue.TryEnqueue(() => items.Add(new(item)
            {
                ListView = MainListView,
                Page = this
            }));
        }

        private void SortList()
        {
            FileSystemEntryView[] pathItems = ObservableCollectionQuickSort.Sort(CurrentFiles);
            CurrentFiles.Clear();
            for (int i = pathItems.Length - 1; i >= 0; i--)
            {
                CurrentFiles.Add(pathItems[i]);
            }
        }

        private void CancelFileTask()
        {
            if (fsCancellationTokenSource != null)
            {
                fsCancellationTokenSource.Cancel();
                fsCancellationTokenSource.Dispose();
                fsCancellationTokenSource = new();
            }
        }

        private void DisplayMessage(string message, bool error = false, bool force = false)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                if (force || eventStopwatch.ElapsedMilliseconds > 100) //ms interval between each notification
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

        private void Refresh()
        {
            fileSystemManager.Reset();
            DisplayFiles(CurrentPath);
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

        private void MainListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is IFileSystemEntry item)
                {
                    //FileSystemSecurity fss = item.GetAccessControl();
#if false
                    BottomBarAccessRightType.Inlines.Add(new Run() { Text = fss.AccessRightType.Name });
                    //BottomBarAccessRuleModified = fss.AccessRules;
                    BottomBarAccessRuleType.Inlines.Add(new Run() { Text = fss.AccessRuleType.Name });
                    BottomBarAreAccessRulesCanonical.Inlines.Add(new Run() { Text = fss.AreAccessRulesCanonical.ToString() });
                    BottomBarAreAccessRulesProtected.Inlines.Add(new Run() { Text = fss.AreAccessRulesProtected.ToString() });
                    BottomBarAreAuditRulesCanonical.Inlines.Add(new Run() { Text = fss.AreAuditRulesCanonical.ToString() });
                    BottomBarAreAuditRulesProtected.Inlines.Add(new Run() { Text = fss.AreAuditRulesProtected.ToString() });
                    //BottomBarAuditRulesModified.Text = fss.AuditRulesModified;
                    BottomBarAuditRuleType.Inlines.Add(new Run() { Text = fss.AuditRuleType.Name });
                    //BottomBarGroupModified
                    //BottomBarIsContainer.Text = fss.IsContainer;
                    //BottomBarIsDS.Text = fss.IsDS;
                    //BottomBarOwnerModified.Text = fss.OwnerModified;
                    BottomBarSecurityDescriptor.Inlines.Add(new Run() { Text = fss.GetSecurityDescriptorSddlForm(AccessControlSections.All) });
#endif
                }
            }
        }

        private async void MainListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MainListView.SelectedItem is IFileSystemEntry item)
            {
                if (item.IsDirectory)
                {
                    DisplayFiles(item.Path);
                }
                else
                {
                    if (await Launcher.LaunchUriAsync(new(item.Path)))
                    {
                        Trace.TraceError("Failed to start resource");
                    }
                }
            }
            e.Handled = true;
        }

        private void MainListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
#if DEBUG
            if ((int)e.Key is (> 64 and < 91) or (> 47 and < 58))
            {
                char keyPressed = default;
                switch (e.Key)
                {
                    case VirtualKey.Cancel:
                        CancelFileTask();
                        return;
                    case VirtualKey.MiddleButton:
                        Debug.WriteLine("Middle button pressed");
                        break;
                    case VirtualKey.XButton1:
                        Debug.WriteLine("XButton1 pressed");
                        break;
                    case VirtualKey.XButton2:
                        Debug.WriteLine("XButton2 pressed");
                        break;
                    case VirtualKey.Enter:
                        if (MainListView.SelectedItem != null)
                        {
                            Debug.WriteLine("Enter pressed, loading: " + CurrentFiles[MainListView.SelectedIndex].Path);
                            DisplayFiles(CurrentFiles[MainListView.SelectedIndex].Path);
                        }
                        return;
                    case VirtualKey.Number0:
                        keyPressed = '0';
                        break;
                    case VirtualKey.Number1:
                        keyPressed = '1';
                        break;
                    case VirtualKey.Number2:
                        keyPressed = '2';
                        break;
                    case VirtualKey.Number3:
                        keyPressed = '3';
                        break;
                    case VirtualKey.Number4:
                        keyPressed = '4';
                        break;
                    case VirtualKey.Number5:
                        keyPressed = '5';
                        break;
                    case VirtualKey.Number6:
                        keyPressed = '6';
                        break;
                    case VirtualKey.Number7:
                        keyPressed = '7';
                        break;
                    case VirtualKey.Number8:
                        keyPressed = '8';
                        break;
                    case VirtualKey.Number9:
                        keyPressed = '9';
                        break;
                    case VirtualKey.A:
                        keyPressed = 'A';
                        break;
                    case VirtualKey.B:
                        keyPressed = 'B';
                        break;
                    case VirtualKey.C:
                        keyPressed = 'C';
                        break;
                    case VirtualKey.D:
                        keyPressed = 'D';
                        break;
                    case VirtualKey.E:
                        keyPressed = 'E';
                        break;
                    case VirtualKey.F:
                        keyPressed = 'F';
                        break;
                    case VirtualKey.G:
                        keyPressed = 'G';
                        break;
                    case VirtualKey.H:
                        keyPressed = 'H';
                        break;
                    case VirtualKey.I:
                        keyPressed = 'I';
                        break;
                    case VirtualKey.J:
                        keyPressed = 'J';
                        break;
                    case VirtualKey.K:
                        keyPressed = 'K';
                        break;
                    case VirtualKey.L:
                        keyPressed = 'L';
                        break;
                    case VirtualKey.M:
                        keyPressed = 'M';
                        break;
                    case VirtualKey.N:
                        keyPressed = 'N';
                        break;
                    case VirtualKey.O:
                        keyPressed = 'O';
                        break;
                    case VirtualKey.P:
                        keyPressed = 'P';
                        break;
                    case VirtualKey.Q:
                        keyPressed = 'Q';
                        break;
                    case VirtualKey.R:
                        keyPressed = 'R';
                        break;
                    case VirtualKey.S:
                        keyPressed = 'S';
                        break;
                    case VirtualKey.T:
                        keyPressed = 'T';
                        break;
                    case VirtualKey.U:
                        keyPressed = 'U';
                        break;
                    case VirtualKey.V:
                        keyPressed = 'V';
                        break;
                    case VirtualKey.W:
                        keyPressed = 'W';
                        break;
                    case VirtualKey.X:
                        keyPressed = 'X';
                        break;
                    case VirtualKey.Y:
                        keyPressed = 'Y';
                        break;
                    case VirtualKey.Z:
                        keyPressed = 'Z';
                        break;
                    case VirtualKey.GoBack:
                        Back();
                        return;
                    case VirtualKey.GoForward:
                        Next();
                        return;
                    case VirtualKey.Refresh:
                        Refresh();
                        return;
                    case VirtualKey.Search:
                        // give focus to the autosuggest box
                        break;
                }
                if (keyPressed != default)
                {
                    int selectIndex = MainListView.SelectedIndex;
                    char charIndex = CurrentFiles[selectIndex].Name[0];
                    if (charIndex == keyPressed)
                    {
                        for (int i = 0; i < CurrentFiles.Count; i++)
                        {
                            if (CurrentFiles[i].Name[0] == keyPressed && i < selectIndex)
                            {
                                MainListView.SelectedIndex = i;
                                return;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < CurrentFiles.Count; i++)
                        {
                            if (CurrentFiles[i].Name[0] == keyPressed)
                            {
                                if (i != selectIndex)// no unnecessary actions
                                {
                                    MainListView.SelectedIndex = i;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

        private void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
        {
            Debug.WriteLine("Char received: " + args.Character);
            args.Handled = true;
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
            CancelFileTask();
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
            catch (UnauthorizedAccessException e)
            {
                Trace.TraceError(e.ToString());
            }
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                DisplayFiles(CurrentPath);
            }
        }

        private void OnMainWindowClosed(object sender, WindowEventArgs args)
        {
            App.Settings.SaveSetting(nameof(ExplorerPage), nameof(CurrentPath), CurrentPath);
            CancelFileTask();
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

        private void FileSystemManager_Change(object sender, ChangeEventArgs data)
        {
            switch (data.ChangeTypes)
            {
                case ChangeTypes.FileCreated:
                    //AddDelegate(CurrentFiles, data.Entry);
                    break;
                case ChangeTypes.FileDeleted:
                    for (int i = 0; i < CurrentFiles.Count; i++)
                    {
                        if (CurrentFiles[i].FileSystemEntry.Equals(data.Entry))
                        {
                            _ = DispatcherQueue.TryEnqueue(() => CurrentFiles.RemoveAt(i));
                            return;
                        }
                    }
                    break;
                case ChangeTypes.DirectoryCreated:
                    Trace.TraceWarning("Directory created");
                    break;
                case ChangeTypes.PathDeleted:
                    DispatcherQueue.TryEnqueue(() => CurrentFiles.Clear());
                    Trace.TraceWarning("Path deleted");
                    break;
            }
        }
        #endregion

        #endregion
    }
}
