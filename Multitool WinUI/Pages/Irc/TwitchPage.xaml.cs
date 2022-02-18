using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data.Settings;
using Multitool.Data.Settings.Converters;
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
using System.Threading.Tasks;

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
        #endregion

        #region private
        private void SavePage()
        {
            if (!saved)
            {
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
                    App.TraceError(ex);
                }
#endif
            }
        }

        /*private async Task CreateWebView()
        {
            if (PageWebView == null)
            {
                PageWebView = new()
                {
                    DefaultBackgroundColor = Colors.Black
                };
                await PageWebView.EnsureCoreWebView2Async();
                PageWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                Grid.SetColumn(PageWebView, 0);
                Grid.SetRow(PageWebView, 1);
                WebViewGrid.Children.Add(PageWebView);
            }
        }*/

        /*private void DestroyWebView()
        {
            return;
            if (PageWebView != null)
            {
                PageWebView.Source = null;
                PageWebView.CoreWebView2.Stop();
                PageWebView.Close();
                WebViewGrid.Children.Remove(PageWebView);
                PageWebView = null;
            }
        }*/

        private void CoreWebView2_DocumentTitleChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
        {
            Debug.WriteLine(args);
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

                        _ = proxy.FetchGlobalEmotes();
                        //_ = proxy.FetchChannelEmotes("xqcow");
                    }
                }
                else
                {
                    App.TraceInformation("Please add your account to chat");
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e) { }

        private void OnMainWindowClose(object sender, WindowEventArgs args)
        {
            SavePage();
        }

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
                    IIrcClient client = new TwitchIrcClient(token, true)
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
                    sender.TabItems.Add(tab);
                    sender.SelectedIndex = -1;
                }
                catch (ArgumentNullException)
                {
                    App.TraceWarning("Login is empty");
                }
            }
        }

        private void Chats_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (sender.TabItems.Contains(args.Tab))
            {
                sender.TabItems.Remove(args.Tab);
            }
        }

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateTo("https://www." + args.QueryText);
        }

        private void UriTextBox_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

        }
        #endregion

        #endregion
    }
}
