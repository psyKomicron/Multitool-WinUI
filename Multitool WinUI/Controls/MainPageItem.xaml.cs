using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class MainPageItem : UserControl
    {
        public MainPageItem()
        {
            InitializeComponent();

            TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(MainPageItem), new(string.Empty));
        }

        public DependencyProperty TextProperty { get; }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public event TypedEventHandler<MainPageItem, RoutedEventArgs> Click;

        private void ControlButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
