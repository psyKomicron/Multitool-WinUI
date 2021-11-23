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
            Message = message.ToString();
            NameColor = new(message.Author.NameColor);
        }

        public string Timestamp { get; }
        public string Message { get; }
        public string UserName { get; }
        public SolidColorBrush NameColor { get; }
    }
}
