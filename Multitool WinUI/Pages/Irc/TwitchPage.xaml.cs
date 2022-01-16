using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Windows.Web.Http;
using System.ComponentModel;
using Multitool.DAL.Settings;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Multitool.Net.Imaging;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TwitchPage : Page, INotifyPropertyChanged
    {
        //private readonly object _lock = new();
        private bool saved;
        private TwitchConnectionToken token;

        public TwitchPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            App.MainWindow.Closed += OnMainWindowClose;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        [Setting("")]
        public string Login { get; set; }

        [Setting(true)]
        public bool RequestTags { get; set; }

        [Setting]
        public bool LoadWebView { get; set; }

        [Setting("twitch.tv")]
        public string LastVisited { get; set; }

        [Setting(500)]
        public int ChatMaxNumberOfMessages { get; set; }

        public List<string> Channels { get; set; }

        public ObservableCollection<TabViewItem> Tabs { get; } = new();
        #endregion

        #region private
        private void SavePage()
        {
            if (!saved)
            {
                if (Channels == null)
                {
                    Channels = new();
                }
                foreach (var tab in Tabs)
                {
                    if (tab.Content is ChatControl control && !Channels.Contains(control.Channel))
                    {
                        Channels.Add(control.Channel);
                    }
                }
                App.Settings.Save(this);
                saved = true;
            }
        }

        private void NavigateTo(string uri)
        {
            try
            {
                PageWebView.Source = new(uri);
            }
            catch (UriFormatException ex)
            {
#if DEBUG
                if (_contentLoaded)
                {
                    App.TraceError(ex.ToString());
                }
                else
                {
                    Debug.WriteLine(uri + "\n" + ex.ToString());
                }
#endif
            }
        }
        #endregion

        #region event handlers

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Settings.Load(this);
                PropertyChanged(this, new(string.Empty));

                if (LoadWebView)
                {
                    NavigateTo($"https://{LastVisited}");
                }

                if (!string.IsNullOrEmpty(Login))
                {
                    token = new(Login);
                    if (!await token.ValidateToken())
                    {
                        App.TraceWarning("Your twitch connection token is not valid. Generate one, or check if the current one is the right one.");
                    }
                    else
                    {
                        TwitchEmoteProxy proxy = TwitchEmoteProxy.GetInstance();
                        proxy.CreateEmoteFetcher(token);
                        proxy.EmoteFetcher.DefaultImageSize = ImageSize.Big;

                        Task<List<Emote>>[] tasks = new Task<List<Emote>>[3];
                        tasks[0] = proxy.EmoteFetcher.GetGlobal7TVEmotes();
                        tasks[1] = proxy.GetGlobalEmotes();
                        tasks[2] = proxy.EmoteFetcher.GetGlobalFfzEmotes();

                        await Task.WhenAll(tasks);

                        tasks[1].Result.AddRange(tasks[0].Result);
                        tasks[1].Result.AddRange(tasks[2].Result);
                    }
                }
                else
                {
                    App.TraceInformation("Please add your account to chat");
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex.ToString());
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e) => SavePage();

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //PageWebView.Close();
            base.OnNavigatedFrom(e);
            /*PageWebView.ExecuteScriptAsync("window.close()").AsTask()
                .ContinueWith((Task<string> task) =>
                {
                    if (task.IsFaulted)
                    {
                        Trace.TraceError(task.Exception.ToString());
                    }
                    else
                    {
                        Trace.TraceInformation("Successfully closed window");
                    }
                });*/
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            /*if (PageWebView.CoreWebView2 != null)
            {
                PageWebView.CoreWebView2.Resume();
            }*/
        }

        private void OnMainWindowClose(object sender, WindowEventArgs args) => SavePage();

        #region ui events
        private void Chats_AddTabButtonClick(TabView sender, object args)
        {
            if (token == null || !token.Validated)
            {
                App.TraceWarning("Cannot connect to any chat without login");
            }
            else
            {
                try
                {
                    ITwitchIrcClient client = new TwitchIrcClient(token, true)
                    {
                        NickName = "psykomicron",
                        Encoding = Encoding.UTF8,
                        RequestTags = RequestTags
                    };

                    TabViewItem tab = new();
                    ChatControl chat = new()
                    {
                        Client = client,
                        Tab = tab,
                        MaxMessages = ChatMaxNumberOfMessages,
                        EmoteSize = ChatEmoteSize_Slider.Value
                    };
                    tab.Content = chat;

                    Tabs.Add(tab);

                    //sender.SelectedIndex = Tabs.Count - 1;
                }
                catch (ArgumentNullException)
                {
                    App.TraceError("Login is empty");
                }
            }
        }

        private void Chats_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => Tabs.Remove(args.Tab);

        private void LoadOAuth_Click(object sender, RoutedEventArgs e)
        {
            if (token.Validated)
            {
                PageWebView.Source = new(@"https://id.twitch.tv/oauth2/authorize?client_id=" + token.ClientId.ToString() + @"&redirect_uri=http://localhost&scope&response_type=token&scope=");
            }
        }

        private void Flyout_Opening(object sender, object e)
        {
            SettingsButton.BorderBrush = new SolidColorBrush(Helpers.Tool.GetAppRessource<Color>("SystemAccentColor"));
        }

        private void Flyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            SettingsButton.BorderBrush = null;
        }

        private async void ValidateTokenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (token == null)
                {
                    token = new(Login);
                }

                if (await token.ValidateToken())
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoginPasswordBox.BorderBrush = new SolidColorBrush(Colors.Green);
                    });
                }
                else
                {
                    Trace.TraceWarning("Token is not valid, try creating a new one or check if it is the right one");
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

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateTo("https://www." + args.QueryText);
        }

        private void PasswordBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Login = LoginPasswordBox.Password;
            }
        }

        private void ChatHistoryLength_Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            foreach (var tab in Tabs)
            {
                if (tab.Content is ChatControl control)
                {
                    control.MaxMessages = (int)e.NewValue;
                }
            }
        }

        private void ChatEmoteSize_Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            foreach (var tab in Tabs)
            {
                if (tab.Content is ChatControl control)
                {
                    control.EmoteSize = e.NewValue;
                }
            }
        }
        #endregion

        #endregion
    }
}
