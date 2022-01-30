using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL.Settings;
using Multitool.Net.Imaging;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TwitchPage : Page, INotifyPropertyChanged
    {
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
        public bool LoadWebView { get; set; }

        [Setting("twitch.tv")]
        public string LastVisited { get; set; }

        [Setting(500)]
        public int ChatMaxNumberOfMessages { get; set; }

        [Setting(30)]
        public int ChatEmoteSize { get; set; }

        [Setting(typeof(RegexSettingConverter), DefaultInstanciate = false)]
        public Regex ChatMentionRegex { get; set; }

        [Setting("t")]
        public string TimestampFormat { get; set; }

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
                        EmoteProxy proxy = EmoteProxy.Get();
                        proxy.EmoteFetchers.Add(new TwitchEmoteFetcher(token));
                        proxy.EmoteFetchers.Add(new FfzEmoteFetcher());
                        proxy.EmoteFetchers.Add(new SevenTVEmoteFetcher());

                        var emotes = await proxy.FetchGlobalEmotes();
                    }
                }
                else
                {
                    App.TraceInformation("Please add your account to chat");
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.TraceError(ex.ToString());
#else
                App.TraceError(ex.ToString());
#endif
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
            //base.OnNavigatedTo(e);
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
                    Encoding encoding = Encoding.Default;
                    //encoding.DecoderFallback = new DecoderReplacementFallback("");
                    ITwitchIrcClient client = new TwitchIrcClient(token, true)
                    {
                        NickName = "psykomicron",
                        Encoding = encoding
                    };

                    TabViewItem tab = new()
                    {
                        MaxWidth = 200
                    };
                    ChatControl chat = new(client)
                    {
                        Tab = tab,
                        MentionRegex = ChatMentionRegex,
                        MaxMessages = ChatMaxNumberOfMessages
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

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateTo("https://www." + args.QueryText);
        }
        #endregion

        #endregion
    }
}
