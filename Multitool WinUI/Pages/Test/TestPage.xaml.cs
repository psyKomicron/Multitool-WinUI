using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Drawing;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Factories;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Models;
using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        //private TwitchIrcClient client;

        public TestPage()
        {
            InitializeComponent();
        }

        public List<string> Items { get; } = new() { "https", "www", "twitch", "tv", "buddha" };

        private void BreadcrumbBar_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void BreadcrumbBar_LostFocus(object sender, RoutedEventArgs e)
        {

        }
    }
}
