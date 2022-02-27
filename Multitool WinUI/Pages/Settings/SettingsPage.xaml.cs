using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data.Settings;
using Multitool.Data.Settings.Converters;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Explorer;
using MultitoolWinUI.Pages.Irc;
using MultitoolWinUI.Pages.Test;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private readonly ISettingsManager settingsManager = App.Settings;
        private string typeToNavigateTo;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        [Setting(true)]
        public bool AutoLoadPageSetting { get; set; }

        public Uri GithubUri { get; } = new(Properties.Resources.GithubUri);

        public SettingsHolder Holder { get; set; }

        public bool ClearHistoryButtonEnabled { get; set; }
        #endregion

        private bool NavigateTo(string tag)
        {
            bool success;
            if (AutoLoadPageSetting)
            {
                switch (tag)
                {
                    case "ExplorerPage":
                        ExplorerHeader.StartBringIntoView();
                        success = true;
                        break;
                    case "TwitchPage":
                        TwitchHeader.Focus(FocusState.Programmatic);
                        success = true;
                        break;
                    case "General":
                    default:
                        GeneralHeader.StartBringIntoView();
                        success = true;
                        break;
                }
            }
            else
            {
                success = false;
            }
            return success;
        }

        private void LoadSettings()
        {
            Holder = new();
            settingsManager.Load(Holder);
            PropertyChanged?.Invoke(this, new(string.Empty));
        }

        #region navigation events
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.Settings.Load(this);
            #region navigation
            if (e.Parameter is string page)
            {
                // handle navigation to the wanted page
                Debug.WriteLine($"SettingsPage: {page}");
            }
            else if (e.Parameter is Type type)
            {
                typeToNavigateTo = type.Name;
            }
            #endregion

            Grid main = App.MainWindow.Content as Grid;
            if (main != null)
            {
                switch (main.RequestedTheme)
                {
                    case ElementTheme.Default:
                        DefaultThemeButton.IsChecked = true;
                        break;
                    case ElementTheme.Light:
                        LightThemeButton.IsChecked = true;
                        break;
                    case ElementTheme.Dark:
                        DarkThemeButton.IsChecked = true;
                        break;
                }
            }
            LoadSettings();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            App.Settings.Save(this);
        }
        #endregion

        #region page event handlers
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (typeToNavigateTo != null)
            {
                if (!NavigateTo(typeToNavigateTo))
                {
                    App.TraceWarning($"Setting page not found for {typeToNavigateTo}");
                }
            }
            typeToNavigateTo = null;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settingsManager.Save(Holder);
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Error occured when saving settings");
            }
        }

        private async void OpenSettingsFile_Click(object sender, RoutedEventArgs e) => await Launcher.LaunchUriAsync(new(App.Settings.SettingFilePath));

        private void DarkThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Grid main = App.MainWindow.Content as Grid;
            if (main != null)
            {
                if (!DarkThemeButton.IsChecked.GetValueOrDefault(false))
                {
                    main.RequestedTheme = ElementTheme.Default;
                    DefaultThemeButton.IsChecked = true;
                }
                else
                {
                    main.RequestedTheme = ElementTheme.Dark;

                    if (LightThemeButton.IsChecked == true)
                    {
                        LightThemeButton.IsChecked = false;
                    }
                    if (DefaultThemeButton.IsChecked == true)
                    {
                        DefaultThemeButton.IsChecked = false;
                    }
                }
            }
        }

        private void LightThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Grid main = App.MainWindow.Content as Grid;
            if (main != null)
            {
                if (!LightThemeButton.IsChecked.GetValueOrDefault(false))
                {
                    main.RequestedTheme = ElementTheme.Default;
                    DefaultThemeButton.IsChecked = true;
                }
                else
                {
                    main.RequestedTheme = ElementTheme.Light;

                    if (DarkThemeButton.IsChecked == true)
                    {
                        DarkThemeButton.IsChecked = false;
                    }
                    if (DefaultThemeButton.IsChecked == true)
                    {
                        DefaultThemeButton.IsChecked = false;
                    }
                }
            }
        }

        private void DefaultThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DefaultThemeButton.IsChecked.GetValueOrDefault(false))
            {
                Grid main = App.MainWindow.Content as Grid;
                if (main != null)
                {
                    main.RequestedTheme = ElementTheme.Default;
                }

                if (DarkThemeButton.IsChecked == true)
                {
                    DarkThemeButton.IsChecked = false;
                }
                if (LightThemeButton.IsChecked == true)
                {
                    LightThemeButton.IsChecked = false;
                }
            }
        }

        private async void ValidateTokenButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Holder.Login))
            {
                App.TraceWarning("Login is empty");
                return;
            }

            try
            {
                TwitchConnectionToken token = new(Holder.Login);

                TokenValidationProgressRing.IsIndeterminate = true;
                TokenValidationProgressRing.Visibility = Visibility.Visible;
                bool valid = await token.ValidateToken();
                TokenValidationProgressRing.IsIndeterminate = false;
                TokenValidationProgressRing.Visibility = Visibility.Collapsed;

                Microsoft.UI.Dispatching.DispatcherQueueTimer timer = DispatcherQueue.CreateTimer();
                timer.Interval = TimeSpan.FromMilliseconds(3_000);
                timer.IsRepeating = false;
                timer.Tick += (s, e) => LoginPasswordBox.BorderBrush = null;
                timer.Start();

                if (valid)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoginPasswordBox.BorderBrush = new SolidColorBrush(Colors.Green);
                    });
                }
                else
                {
                    App.TraceWarning("Token is not valid, try creating a new one or check if it is the right one");
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoginPasswordBox.BorderBrush = new SolidColorBrush(Colors.IndianRed);
                    });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private void ChatEmoteSize_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            //int value = (int)e.NewValue;
            //settingsManager.EditSetting(typeof(TwitchPage).FullName, , value);
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            /*string value = LoginPasswordBox.Password;
            settingsManager.EditSetting(typeof(TwitchPage).FullName, "ChatMaxNumberOfMessages", value);*/
        }

        private void LoadOAuth_Click(object sender, RoutedEventArgs e)
        {
            // load webpage in a webview (maybe new window ?)
            /*if (token.Validated)
            {
                PageWebView.Source = new(@"https://id.twitch.tv/oauth2/authorize?client_id=" + token.ClientId.ToString() + @"&redirect_uri=http://localhost&scope&response_type=token&scope=");
            }*/
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.RemoveSetting(typeof(ExplorerPage).FullName, "History");
        }

        private void ResetSettingsFile_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.Reset();
        }

        private void ShowMentionsHelp_Click(object sender, RoutedEventArgs e)
        {
            MentionsTeachingTip.IsOpen = !MentionsTeachingTip.IsOpen;
        }

        private async void TempDataFolderHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new(ApplicationData.Current.TemporaryFolder.Path));
        }

        private async void AppDataFolderHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new(ApplicationData.Current.LocalFolder.Path));
        }

        private void AppSettingsFolderHyperlink_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void ClearTempFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = await ApplicationData.Current.TemporaryFolder.GetFilesAsync();
                List<Task> tasks = new();
                foreach (var file in files)
                {
                    tasks.Add(file.DeleteAsync().AsTask());
                }
                await Task.WhenAll(tasks);
                App.TraceInformation("Cleared temporary folder.");
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }
        #endregion
    }

    internal record SettingsHolder
    {
        #region Twitch
        [Setting(typeof(ChatControl), nameof(ChatControl.MaxMessages))]
        public int ChatMaxNumberOfMessages { get; set; }

        [Setting(typeof(ChatControl), nameof(ChatControl.Mention), typeof(RegexSettingConverter))]
        public Regex ChatMentionRegex { get; set; }

        [Setting(typeof(ChatControl), nameof(ChatControl.EmoteSize))]
        public int EmoteSize { get; set; }

        [Setting(typeof(TwitchPage), nameof(TwitchPage.LoadWebView))]
        public bool LoadWebView { get; set; }

        [Setting(typeof(TwitchPage), nameof(TwitchPage.Login))]
        public string Login { get; set; }

        [Setting(typeof(ChatControl), nameof(ChatControl.TimestampFormat))]
        public string MessageTimestampFormat { get; set; } 
        #endregion

        #region Explorer
        [Setting(typeof(ExplorerPage), nameof(ExplorerPage.LoadLastPath))]
        public bool LoadLastPath { get; set; }

        [Setting(typeof(ExplorerPage), nameof(ExplorerPage.KeepHistory))]
        public bool KeepHistory { get; set; }
        #endregion

        #region Main page
        // Main page
        [Setting(typeof(MainPage), nameof(MainPage.LoadShortcuts))]
        public bool MainPageLoadShortcuts { get; set; }
        #endregion

        #region Main window
        [Setting(typeof(MainWindow), nameof(MainWindow.CurrentTheme))]
        public ElementTheme ApplicationTheme { get; set; }
        #endregion

        #region Spotlight importer
        [Setting(typeof(SpotlightImporter), nameof(SpotlightImporter.CollisionOption))]    
        public CreationCollisionOption SpotlightCollisionOption { get; set; }

        [Setting(typeof(SpotlightImporter), nameof(SpotlightImporter.MenuBarLabelPosition))]
        public bool SpotlightDeleteTempData { get; set; }

        [Setting(typeof(SpotlightImporter), nameof(SpotlightImporter.MenuBarLabelPosition))]
        public CommandBarDefaultLabelPosition SpotlightMenuBarLabelPosition { get; set; }

        [Setting(typeof(SpotlightImporter), nameof(SpotlightImporter.OpenTempFolder))]
        public bool SpotlightOpenTempFolder { get; set; } 
        #endregion
    }
}
