using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;

using System.Collections.Generic;

namespace MultitoolWinUI.Pages.Irc
{
    public record ChatPageParameter(ITwitchIrcClient Client, TabViewItem Tab, string Channel, List<Emote> emotes);
}
