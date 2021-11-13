using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using Multitool.DAL;
using Multitool.Net.Irc;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IrcChatPage : Page, IDisposable
    {
        private const string twitchUrl = "https://twitch.tv/";
        private readonly object _lock = new();
        private readonly System.Timers.Timer timer = new()
        {
            Interval = 5000,
            AutoReset = true
        };
        private readonly TwitchIrcClient ircClient;
        private bool saved;
        private long messages;

        public IrcChatPage()
        {
            InitializeComponent();
            try
            {
                ISettings settings = App.Settings;
                Login = settings.GetSetting<string>(nameof(IrcChatPage), nameof(Login));
                ircClient = new(Login)
                {
                    NickName = "psykomicron",
                    Encoding = Encoding.UTF8
                };
                ircClient.MessageReceived += IrcClient_MessageReceived;
                LastStream = settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
            }
            catch (SettingNotFoundException) 
            {
                LastStream = string.Empty;
            }

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            App.MainWindow.Closed += OnMainWindowClose;
            Chat.CollectionChanged += Chat_CollectionChanged;
            timer.Elapsed += Timer_Elapsed;
        }

        #region properties
        public string LastStream { get; set; }

        public ObservableCollection<ChatMessageModel> Chat { get; set; } = new();

        public string Login { get; set; }
        #endregion

        #region public
        public void Dispose()
        {
            ircClient.Dispose();
        }
        #endregion

        #region private
        private void SavePage()
        {
            if (!saved)
            {
                ISettings settings = App.Settings;
                settings.SaveSetting(nameof(IrcChatPage), nameof(LastStream), LastStream);
                settings.SaveSetting(nameof(IrcChatPage), nameof(Login), Login);
                saved = true;
            }
        }

        private async void NavigateTo(string uri)
        {
            try
            {
                PageWebView.Source = new(twitchUrl + uri);
            }
            catch (UriFormatException ex)
            {
#if DEBUG
                if (_contentLoaded)
                {
                    Trace.TraceError(ex.ToString());
                }
                else
                {
                    Debug.WriteLine(uri + "\n" + ex.ToString());
                }
#endif
            }
            try
            {
                if (ircClient != null && ircClient.ClientState == System.Net.WebSockets.WebSocketState.Open)
                {
                    await ircClient.Part(LastStream);
                    await ircClient.Join(uri);
                }
            }
            catch (ArgumentException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private string StringifyHeaders(CoreWebView2HttpRequestHeaders requestHeaders)
        {
            StringBuilder builder = new();
            foreach (KeyValuePair<string, string> header in requestHeaders)
            {
                builder.Append('[').Append(header.Key).Append(" | ").Append(header.Value).Append(']').Append('\n');
            }
            builder.Length--;
            return builder.ToString();
        }

        private async Task ExitPage()
        {
            timer.Stop();
            if (ircClient != null)
            {
                ircClient.MessageReceived -= IrcClient_MessageReceived;
#if true
                if (ircClient.Connected)
                {
                    await ircClient.Part(LastStream);
                }
#endif
                await ircClient.Disconnect();
            }
            SavePage();
        }
        #endregion

        #region event handlers
        private void IrcClient_MessageReceived(IIrcClient sender, string args)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    lock (_lock)
                    {
#if false
                        if (Chat.Count > 0 && Chat.Count > MaxNumberOfMessages)
                        {
                            for (int i = 0; i < DeleteChunk && Chat.Count > 0; i++)
                            {
                                Chat.RemoveAt(0);
                            }
                        }
#endif
                        Chat.Add(new(string.Empty, args));
                    }
                });
            }
            Interlocked.Increment(ref messages);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Read(ref messages) > 0)
            {
                long copy = Interlocked.Read(ref messages) / 5L;
                Interlocked.Exchange(ref messages, 0);
                _ = DispatcherQueue.TryEnqueue(() => MPSTextBlock.Text = copy.ToString());
            }
        }

        private void Chat_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DispatcherQueue?.TryEnqueue(() => NoMTextBlock.Text = Chat.Count.ToString());
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            timer.Start();
            try
            {
                LastStream = App.Settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
                NavigateTo(LastStream);
            }
            catch (SettingNotFoundException ex)
            {
                Trace.TraceError(ex.ToString());
            }

            if (ircClient != null)
            {
                await ircClient.Connect(new(@"wss://irc-ws.chat.twitch.tv:443"));
                if (!string.IsNullOrEmpty(LastStream))
                {
                    await ircClient.Join(LastStream);
                }
            }
        }

        private async void OnPageUnloaded(object sender, RoutedEventArgs e) => await ExitPage();

        private async void OnMainWindowClose(object sender, WindowEventArgs args) => await ExitPage();

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateTo(args.QueryText);
            LastStream = args.QueryText;
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            Chat.Clear();
            Interlocked.Exchange(ref messages, 0);
            Trace.TraceInformation("Cleared chat");
        }

        private async void LeaveChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ircClient.Part(LastStream);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            //await ircClient.Part(LastStream);
        }
        #endregion
    }
}
