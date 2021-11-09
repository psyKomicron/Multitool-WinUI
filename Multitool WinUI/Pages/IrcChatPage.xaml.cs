using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using Multitool.DAL;
using Multitool.Net.Irc;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Windows.Media.Protection.PlayReady;

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
            Interval = 1000,
            AutoReset = true
        };
        private readonly TwitchIrcClient ircClient = new(@"oauth:")
        {
            NickName = "psykomicron"
        };
        private bool saved;
        private long messages;

        public IrcChatPage()
        {
            InitializeComponent();
            try
            {
                ISettings settings = App.Settings;
                LastStream = settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
            }
            catch (SettingNotFoundException) 
            {
                LastStream = string.Empty;
            }

            Loaded += OnPageLoaded;
            App.MainWindow.Closed += OnMainWindowClose;
            Chat.CollectionChanged += Chat_CollectionChanged;
            timer.Elapsed += Timer_Elapsed;
            ircClient.MessageReceived += IrcClient_MessageReceived;
        }

        #region public

        #region properties
        public string LastStream { get; set; }

        public ObservableCollection<string> Chat { get; set; } = new();

        public int MaxNumberOfMessages { get; set; }

        public int DeleteChunk { get; set; }
        #endregion

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
                saved = true;
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

        private void NavigateTo(string uri)
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
        }
        #endregion

        #region event handlers
        private void IrcClient_MessageReceived(IIrcClient sender, string args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                lock (_lock)
                {
                    if (Chat.Count > 0 && Chat.Count > MaxNumberOfMessages)
                    {
                        for (int i = 0; i < DeleteChunk && Chat.Count > 0; i++)
                        {
                            Chat.RemoveAt(0);
                        }
                    }
                    Chat.Add(DateTime.Now.ToString() + " : " + args);
                }
                Interlocked.Increment(ref messages);
            });
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Read(ref messages) > 0)
            {
                long copy = Interlocked.Read(ref messages);
                Interlocked.Exchange(ref messages, 0);
                _ = DispatcherQueue.TryEnqueue(() => MPSTextBlock.Text = copy.ToString());
            }
        }

        private void Chat_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => NoMTextBlock.Text = Chat.Count.ToString());
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            //timer.Start();
            try
            {
                LastStream = App.Settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
                NavigateTo(LastStream);
            }
            catch (SettingNotFoundException ex)
            {
                Trace.TraceError(ex.ToString());
}

            await ircClient.Connect(new(@"wss://irc-ws.chat.twitch.tv:443"));
            await ircClient.Join(LastStream);
        }

        private void OnMainWindowClose(object sender, WindowEventArgs args)
        {
            _ = ircClient.Disconnect();
            _ = ircClient.Part(LastStream);

            Chat.Clear();
            SavePage();
        }

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            //Trace.TraceInformation("Query submitted : " + args.QueryText);
            LastStream = args.QueryText;
            NavigateTo(args.QueryText);
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            Chat.Clear();
            Interlocked.Exchange(ref messages, 0);
            Trace.TraceInformation("Cleared chat");
        }

        private void ChatListView_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Scrolling");
        }

        private void PageWebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            //Debug.WriteLine($"/!\\Navigation started\nnavigation id : {args.NavigationId}\nuri : {args.Uri}\nrequest headers : \n{StringifyHeaders(args.RequestHeaders)}\nis redirected : {args.IsRedirected}\nis user initialiased : {args.IsUserInitiated}\n");
        }

        private void PageWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            //Debug.WriteLine($"/!\\Navigation completed\nnavigation id : {args.NavigationId}\nsuccess : {args.IsSuccess}\nerror status : {args.WebErrorStatus}\n");
        }

        private void PageWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            //Debug.WriteLine($"Message received, source {args.Source} : \n{args.WebMessageAsJson}\n");
        }
        #endregion
    }
}
