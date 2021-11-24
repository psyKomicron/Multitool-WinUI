using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL;
using Multitool.Net.Twitch;

using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IrcChatPage : Page
    {
        private const string twitchUrl = "https://twitch.tv/";
        private readonly object _lock = new();
        private bool saved;

        public IrcChatPage()
        {
            InitializeComponent();
            try
            {
                ISettings settings = App.Settings;
                Login = settings.GetSetting<string>(nameof(IrcChatPage), nameof(Login));
                LastStream = settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
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
                settings.SaveSetting(nameof(IrcChatPage), nameof(LastStream), LastStream);
                settings.SaveSetting(nameof(IrcChatPage), nameof(Login), Login);
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
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LastStream = App.Settings.GetSetting<string>(nameof(IrcChatPage), nameof(LastStream));
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

        private void Chats_AddTabButtonClick(TabView sender, object args)
        {
            if (string.IsNullOrEmpty(Login))
            {
                Trace.TraceWarning("Cannot connect to any chat without login");
            }
            else
            {
                try
                {
                    TabViewItem tab = new()
                    {
                        Header = LastStream
                    };
                    IIrcClient client = new TwitchIrcClient(new TwitchConnectionToken(Login))
                    {
                        NickName = "psykomicron",
                        Encoding = Encoding.UTF8,
                        RequestTags = false
                    };
                    Frame frame = new();

                    tab.Content = frame;
                    frame.Navigate(typeof(ChatPage), new ChatPageParameter(client, tab, LastStream));
                    sender.TabItems.Add(tab);
                    sender.SelectedItem = tab;
                }
                catch (FormatException ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        private void Chats_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => Chats.TabItems.Remove(args.Tab);
        #endregion
    }
}
