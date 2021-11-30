using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TwitchChatPage : Page, INotifyPropertyChanged
    {
        private readonly object _lock = new();
        private bool saved;
        private TwitchConnectionToken token;

        public TwitchChatPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            App.MainWindow.Closed += OnMainWindowClose;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        public string LastStream { get; set; }

        public string Login { get; set; }

        public ObservableCollection<object> Tabs { get; } = new();
        #endregion

        #region private
        private void SavePage()
        {
            if (!saved)
            {
                ISettings settings = App.Settings;
                settings.SaveSetting(nameof(TwitchChatPage), nameof(LastStream), LastStream);
                settings.SaveSetting(nameof(TwitchChatPage), nameof(Login), Login);
                saved = true;
            }
        }

        private void NavigateTo(string uri)
        {
            try
            {
                //PageWebView.Source = new(twitchUrl + uri);
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

        private void ExitPage()
        {
            SavePage();
        }
        #endregion

        #region event handlers
        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ISettings settings = App.Settings;
            try
            {
                LastStream = settings.GetSetting<string>(nameof(TwitchChatPage), nameof(LastStream));
                PropertyChanged(this, new(nameof(LastStream)));
                NavigateTo(LastStream);
            }
            catch (SettingNotFoundException)
            {
                LastStream = string.Empty;
            }

            try
            {
                Login = settings.GetSetting<string>(nameof(TwitchChatPage), nameof(Login));
                PropertyChanged(this, new(nameof(Login)));

                token = new(Login);
                await token.ValidateToken();
            }
            catch (SettingNotFoundException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (FormatException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e) => ExitPage();

        private void OnMainWindowClose(object sender, WindowEventArgs args) => ExitPage();

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateTo(args.QueryText);
            LastStream = args.QueryText;
        }

        private void Chats_AddTabButtonClick(TabView sender, object args)
        {
            if (string.IsNullOrEmpty(LastStream))
            {
                Trace.TraceWarning("No channel is set");
                return;
            }

            bool requestTags, loadWebView;
            requestTags = RequestTagsButton.IsOn;
            loadWebView = LoadWebViewButton.IsOn;

            if (string.IsNullOrEmpty(Login))
            {
                Trace.TraceWarning("Cannot connect to any chat without login");
            }
            else
            {
                try
                {
                    ITwitchIrcClient client = new TwitchIrcClient(token, 5_000, true)
                    {
                        NickName = "psykomicron",
                        Encoding = Encoding.UTF8,
                        RequestTags = requestTags,
                    };

                    TabViewItem tab = new()
                    {
                        Header = LastStream
                    };
                    Frame frame = new();
                    tab.Content = frame;
                    frame.Navigate(typeof(ChatPage), new ChatPageParameter(client, tab, LastStream));
                    sender.TabItems.Add(tab);
                    sender.SelectedItem = tab;
                }
                catch (ArgumentNullException)
                {
                    Trace.TraceError("Login is empty");
                }

                if (loadWebView)
                {
                    PageWebView.Source = new(Properties.Resources.TwitchUrl + LastStream);
                }
            }
        }

        private void Chats_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => Chats.TabItems.Remove(args.Tab);

        private async void LoadOAuth_Click(object sender, RoutedEventArgs e)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new("Bearer", Login);
            HttpResponseMessage res = await client.GetAsync(new(@"https://id.twitch.tv/oauth2/validate"));
            if (res.StatusCode == HttpStatusCode.Ok)
            {
                JsonDocument json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("client_id", out JsonElement value))
                {
                    Debug.WriteLine(value.ToString());
                    PageWebView.Source = new(@"https://id.twitch.tv/oauth2/authorize?client_id" + value.ToString() + @"=&redirect_uri=http://localhost&scope&response_type=token&scope=");
                }
            }
            else
            {
                Trace.TraceWarning("Unable to contact twitch to get client id");
            }
        }

        private async void LoadEmotes_Click(object sender, RoutedEventArgs e)
        {
            EmoteFetcher emoteFetcher = new(new(Login));
            List<Emote> emotes = await emoteFetcher.GetAllEmotes();
            foreach (Emote emote in emotes)
            {

            }
        }
        #endregion
    }
}
