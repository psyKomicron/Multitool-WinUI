using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Irc;

using Windows.Foundation;

namespace MultitoolWinUI.Models
{
    public sealed class MessageModel
    {
        public MessageModel(Message message) 
        {
            Message = message;
        }

        public Brush Background { get; set; }
        public FrameworkElement Content { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public Message Message { get; }
        public Image UserBadge { get; set; }

        public event TypedEventHandler<MessageModel, Message> Reply;

        public void OnReply(object sender, object e)
        {
            Reply?.Invoke(this, Message);
        }
    }
}
