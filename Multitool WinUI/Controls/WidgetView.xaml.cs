using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class WidgetView : UserControl
    {
        private bool opened;

        public WidgetView()
        {
            this.InitializeComponent();
        }

        public string WidgetIcon { get; set; }
        public string WidgetName { get; set; }
        public FrameworkElement Widget { get; set; }

        public event TypedEventHandler<WidgetView, RoutedEventArgs> Opened;
        public event TypedEventHandler<WidgetView, RoutedEventArgs> Closed;

        public void Close()
        {
            RemoveWidget();
            Closed?.Invoke(this, null);
            opened = false;
        }

        public void Open(Control control)
        {
            AddControl(control);
            Opened?.Invoke(this, null);
            opened = true;
        }

        public void Open()
        {
            AddControl(Widget);
        }

        public void AddControl(FrameworkElement control)
        {
            mainContentGrid.Visibility = Visibility.Collapsed;
            contentGrid.RowDefinitions[0].Height = new(0);

            additionalGrid.Children.Clear();
            additionalGrid.Visibility = Visibility.Visible;
            additionalGrid.Children.Add(control);
        }

        private void RemoveWidget()
        {
            mainContentGrid.Visibility = Visibility.Visible;
            contentGrid.RowDefinitions[0].Height = new(1, GridUnitType.Star);

            additionalGrid.Children.Clear();
            additionalGrid.Visibility = Visibility.Collapsed;
        }

        private void SpotlightButton_Click(object sender, RoutedEventArgs e)
        {
            if (opened)
            {
                RemoveWidget();
                Closed?.Invoke(this, e);
                opened = false;
            }
            else
            {
                Opened?.Invoke(this, e);
                opened = true;
            }
        }
    }
}
