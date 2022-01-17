using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Imaging;
using Multitool.Net.Twitch;

using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        public TestPage()
        {
            InitializeComponent();
        }

        public ObservableCollection<Emote> Emotes { get; set; } = new();

        public string Channel { get; set; }

        private async void LoadGlobalEmotesButton_Click(object sender, RoutedEventArgs e)
        {
            Emotes.Clear();
            List<Emote> emotes = await TwitchEmoteProxy.GetInstance().GetGlobalEmotes();
            foreach (var emote in emotes)
            {
                Emotes.Add(emote);
            }
        }

        private async void LoadChannelEmotes_Click(object sender, RoutedEventArgs e)
        {
            Emotes.Clear();
            List<Emote> emotes = await TwitchEmoteProxy.GetInstance().GetChannelEmotes(Channel);
            foreach (var emote in emotes)
            {
                Emotes.Add(emote);
            }
        }

        private void EmoteGridView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
