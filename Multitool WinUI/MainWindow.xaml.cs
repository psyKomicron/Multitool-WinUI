using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Pages;

using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : AppWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Multitool";
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(MainPage), this);
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "home":
                        ContentFrame.Navigate(typeof(MainPage), this);
                        break;
                    case "devices":
                        ContentFrame.Navigate(typeof(ComputerDevicesPage), this);
                        break;
                }
            }
        }

        private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }
    }
}
