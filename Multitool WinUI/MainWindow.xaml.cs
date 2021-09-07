using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Pages;
using MultitoolWinUI.Pages.ControlPanels;
using MultitoolWinUI.Pages.Power;

using System;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Multitool";
            Application.Current.UnhandledException += OnUnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("Unhandled exception event fired");
            DisplayMessage((e.ExceptionObject as Exception)?.Message);
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("Unhandled exception event fired");
            DisplayMessage(e.Exception.Message);
        }

        public void DisplayMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ExceptionTextBlock.Text = message;
                    ExceptionPopup.IsOpen = true;
                });
            }
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            _ = ContentFrame.Navigate(typeof(MainPage), this);
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                string tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "home":
                        _ = ContentFrame.Navigate(typeof(MainPage));
                        break;
                    case "devices":
                        _ = ContentFrame.Navigate(typeof(ComputerDevicesPage));
                        break;
                    case "explorer":
                        _ = ContentFrame.Navigate(typeof(ExplorerHomePage));
                        break;
                    case "power":
                        _ = ContentFrame.Navigate(typeof(PowerPage));
                        break;
                    case "controlpanels":
                        _ = ContentFrame.Navigate(typeof(ControlPanelsPage));
                        break;
                    default:
                        Trace.WriteLine("Trying to navigate to: " + tag);
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
