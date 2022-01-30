using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MultitoolWinUI.Models
{
    public sealed class MessageModel
    {
        public MessageModel() { }

        public FrameworkElement Content { get; set; }
        public Brush Background { get; set; }
        public Image UserBadge { get; set; }
    }
}
