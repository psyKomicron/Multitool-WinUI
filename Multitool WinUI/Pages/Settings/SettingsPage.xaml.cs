using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL.Settings;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Explorer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private readonly ISettingsManager settingsManager = App.Settings;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        #region settings
        [Setting(true)]
        public bool AutoLoadPageSetting { get; set; }
        #endregion

        public bool LoadWebView { get; set; }
        public string Login { get; set; }
        public int ChatMaxNumberOfMessages { get; set; }
        public Uri GithubUri { get; set; } = new(Properties.Resources.GithubUri);
        public bool LoadLastPath { get; set; }
        public bool KeepHistory { get; set; }
        public bool ClearHistoryButtonEnabled { get; set; }
        public bool MainPageLoadShortcuts { get; set; }
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
                        TwitchHeader.StartBringIntoView();
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
                string typeName = type.Name;
                if (!NavigateTo(typeName))
                {
                    App.TraceWarning($"Setting page not found for {typeName}");
                }
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

            var settings = settingsManager.ListSettingsKeys(typeof(TwitchPage).FullName);
            if (settings.Contains("LoadWebView") && settings.Contains("Login") && settings.Contains("ChatMaxNumberOfMessages"))
            {
                LoadWebView = settingsManager.GetSetting<bool>(typeof(TwitchPage).FullName, "LoadWebView");
                Login = settingsManager.GetSetting<string>(typeof(TwitchPage).FullName, "Login");
                ChatMaxNumberOfMessages = settingsManager.GetSetting<int>(typeof(TwitchPage).FullName, "ChatMaxNumberOfMessages");
            }

            if (settingsManager.TryGetSetting(typeof(MainPage).FullName, "LoadShortcuts", out bool loadShortcuts))
            {
                MainPageLoadShortcuts = loadShortcuts;
            }
            else
            {
                MainPageLoadShortcuts = true;
                settingsManager.SaveSetting(typeof(MainPage).FullName, "LoadShortcuts", true);
            }

            PropertyChanged?.Invoke(this, new(string.Empty));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            App.Settings.Save(this);
        }
        #endregion

        #region page event handlers
        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.EditSetting(typeof(MainPage).FullName, "LoadShortcuts", LoadShortcutsToggleSwitch.IsOn);

            settingsManager.EditSetting(typeof(ExplorerPage).FullName, nameof(LoadLastPath), LoadLastPath);
            settingsManager.EditSetting(typeof(ExplorerPage).FullName, nameof(KeepHistory), KeepHistory);

            settingsManager.EditSetting(typeof(TwitchPage).FullName, nameof(LoadWebView), LoadWebView);
            settingsManager.EditSetting(typeof(TwitchPage).FullName, nameof(Login), Login);
            settingsManager.EditSetting(typeof(TwitchPage).FullName, nameof(ChatMaxNumberOfMessages), ChatMaxNumberOfMessages);

            settingsManager.Commit();
        }

        private async void OpenSettingsFile_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new(App.Settings.SettingFilePath));
        }

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
            if (string.IsNullOrEmpty(Login))
            {
                App.TraceWarning("Login is empty");
                return;
            }

            try
            {
                TwitchConnectionToken token = new(Login);

                TokenValidationProgressRing.IsIndeterminate = true;
                TokenValidationProgressRing.Visibility = Visibility.Visible;
                bool valid = await token.ValidateToken();
                TokenValidationProgressRing.IsIndeterminate = false;
                TokenValidationProgressRing.Visibility = Visibility.Collapsed;

                DispatcherQueueTimer timer = DispatcherQueue.CreateTimer();
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
            int value = (int)e.NewValue;
            settingsManager.EditSetting(typeof(TwitchPage).FullName, , value);
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            string value = LoginPasswordBox.Password;
            settingsManager.EditSetting(typeof(TwitchPage).FullName, "ChatMaxNumberOfMessages", value);
        }

        private void LoadOAuth_Click(object sender, RoutedEventArgs e)
        {
            // load webpage in a webview (maybe new window ?)
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.RemoveSetting(typeof(ExplorerPage).FullName, "History");
        }

        private void ResetSettingsFile_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.Reset();
        }
        #endregion
    }
}
