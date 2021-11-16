using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using Multitool.DAL;
using Multitool.Net.Irc;

using MultitoolWinUI.Controls;
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

        private async void LeaveChat_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Chats_AddTabButtonClick(TabView sender, object args)
        {
            TabViewItem tab = new();
            IIrcClient client = new TwitchIrcClient(Login)
            {
                NickName = "psykomicron",
                Encoding = Encoding.UTF8
            };
            ChatControl control = new();

            tab.Content = control;
            Chats.TabItems.Add(tab);
        }

        private void Chats_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => Chats.TabItems.Remove(args.Tab);
        #endregion
    }
}
