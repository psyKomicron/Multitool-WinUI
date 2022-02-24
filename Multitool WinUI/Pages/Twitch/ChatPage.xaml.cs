using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Collections;
using Multitool.Data.Settings;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        private readonly DelayedActionQueue queue = new(2_000);
        private InfoBar infoBar;
        //private TabView chats;
        private TwitchConnectionToken connectionToken;

        public ChatPage()
        {
            InitializeComponent();

            queue.DispatcherQueue = DispatcherQueue;
            queue.QueueEmpty += Queue_QueueEmpty;
            Loaded += OnLoaded;

            try
            {
                App.Settings.Load(this);
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Settings for chat page failed to load.");
            }
        }

        [Setting(typeof(TwitchPage), nameof(TwitchPage.Login))]
        public string TwitchLogin { get; set; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //TwitchLogin = App.Settings.GetSetting<string>(typeof(TwitchPage).FullName, nameof(TwitchPage.Login));
                connectionToken = new(TwitchLogin);
                if (!await connectionToken.ValidateToken())
                {
                    App.TraceWarning("The twitch connection token saved is not valid, please re-create one.");
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Could not create connection token.");
            }
            infoBar = (InfoBar)FindName("InfoBar");
            //chats = (TabView)FindName("ChatTabView");
        }

        private void Queue_QueueEmpty(DelayedActionQueue sender, System.Timers.ElapsedEventArgs args)
        {
            infoBar.IsOpen = false;
            infoBar.Title = string.Empty;
            infoBar.Message = string.Empty;
            infoBar.Severity = InfoBarSeverity.Informational;
        }

        private void ChatTabView_AddTabButtonClick(TabView sender, object args)
        {
            try
            {
                if (connectionToken != null)
                {
                    TwitchIrcClient client = new(connectionToken, true);
                    TabViewItem tab = new();
                    ChatControl control = new(client)
                    {
                        Tab = tab
                    };
                    tab.Content = control;
                    sender.TabItems.Add(tab);
                    tab.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Failed to add chat tab.");
            }
        }

        private void ChatTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => sender.TabItems.Remove(args.Tab);
    }
}
