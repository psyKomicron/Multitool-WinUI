using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Twitch;

namespace MultitoolWinUI.Models
{
    public sealed class ChatMessageModel
    {
        private const string dateTimeFormat = "t";

        public ChatMessageModel() { }

        public ChatMessageModel(Message message)
        {
            Timestamp = message.ServerTimestamp.ToString(dateTimeFormat);
            UserName = message.Author.DisplayName;
            Message = new TextBlock()
            {
                Text = message.ToString(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true
            };
            NameColor = new(message.Author.NameColor);
        }

        public string Timestamp { get; set; }
        public FrameworkElement Message { get; set; }
        public string UserName { get; set; }
        public SolidColorBrush NameColor { get; set; }
        public Image UserBadge { get; set; }
    }
}
