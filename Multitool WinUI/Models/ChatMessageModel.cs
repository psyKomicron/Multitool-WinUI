using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Twitch;

namespace MultitoolWinUI.Models
{
    public sealed class ChatMessageModel
    {
        private const string dateTimeFormat = "t";

        public ChatMessageModel()
        {
            Timestamp = string.Empty;
            Message = string.Empty;
            UserName = string.Empty;
        }

        public ChatMessageModel(Message message)
        {
            Timestamp = message.ServerTimestamp.ToString(dateTimeFormat);
            UserName = message.Author.DisplayName;
            Message = new TextBlock()
            {
                Text = message.ToString(),
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true
            };
            NameColor = new(message.Author.NameColor);
        }

        public string Timestamp { get; set; }
        public object Message { get; set; }
        public string UserName { get; set; }
        public SolidColorBrush NameColor { get; set; }
    }
}
