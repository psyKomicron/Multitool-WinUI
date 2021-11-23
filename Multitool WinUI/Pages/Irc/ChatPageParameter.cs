using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Twitch;

namespace MultitoolWinUI.Pages.Irc
{
    public record ChatPageParameter(IIrcClient Client, TabViewItem Tab, string Channel);
}
