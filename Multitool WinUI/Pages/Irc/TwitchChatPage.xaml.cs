using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.ObjectModel;
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
    public sealed partial class TwitchChatPage : Page
    {
        private const string twitchUrl = "https://twitch.tv/";
        private readonly object _lock = new();
        private bool saved;

        public TwitchChatPage()
        {
            InitializeComponent();
            try
            {
                ISettings settings = App.Settings;
                Login = settings.GetSetting<string>(nameof(TwitchChatPage), nameof(Login));
                LastStream = settings.GetSetting<string>(nameof(TwitchChatPage), nameof(LastStream));
            }
            catch (SettingNotFoundException) 
            {
                LastStream = string.Empty;
            }

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            App.MainWindow.Closed += OnMainWindowClose;
        }

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

        private async Task<bool> ValidateToken()
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new("Bearer", Login);
            HttpResponseMessage res = await client.GetAsync(new(@"https://id.twitch.tv/oauth2/validate"));
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Trace.TraceError("Token to connect to Twitch is invalid, create another one to be able to connect chats.");
                return false;
            }
#if false
            JsonElement value;
            JsonDocument json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("login", out value) && value.ValueKind.HasFlag(JsonValueKind.String))
            {
                Debug.WriteLine(value.ToString());
            }
            if (json.RootElement.TryGetProperty("client_id", out value))
            {
                Debug.WriteLine(value.ToString());
            }
#endif
            return true;
        }
        #endregion

        #region event handlers
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LastStream = App.Settings.GetSetting<string>(nameof(TwitchChatPage), nameof(LastStream));
                NavigateTo(LastStream);
            }
            catch (SettingNotFoundException ex)
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

        private async void Chats_AddTabButtonClick(TabView sender, object args)
        {
            if (!await ValidateToken())
            {
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
                    IIrcClient client = new TwitchIrcClient(new TwitchConnectionToken(Login))
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

                    if (loadWebView)
                    {
                        PageWebView.Source = new(twitchUrl + LastStream);
                    }
                }
                catch (FormatException ex)
                {
                    Trace.TraceError(ex.ToString());
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

            }
        }
        #endregion
    }
}
