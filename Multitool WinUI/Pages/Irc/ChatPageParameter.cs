using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Irc;

namespace MultitoolWinUI.Pages.Irc
{
    public record ChatPageParameter(IIrcClient Client, TabViewItem Tab, string Channel);
}
