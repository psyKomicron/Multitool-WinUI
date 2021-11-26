using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;

namespace MultitoolWinUI.Pages.Irc
{
    public record ChatPageParameter(IIrcClient Client, TabViewItem Tab, string Channel);
}
