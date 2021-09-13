using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Pages;
using MultitoolWinUI.Pages.ControlPanels;
using MultitoolWinUI.Pages.Explorer;
using MultitoolWinUI.Pages.HashGenerator;
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
        private Type lastPage;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Multitool";

            Application.Current.UnhandledException += OnUnhandledException;

            try
            {
                IsPaneOpen = Tool.GetSetting<bool>(nameof(MainWindow), nameof(IsPaneOpen));
            }
            catch (SettingNotFoundException)
            {
                IsPaneOpen = true;
            }

            try
            {
                string lastPageName = Tool.GetSetting<string>(nameof(MainWindow), nameof(lastPage));
                switch (lastPageName)
                {
                    case nameof(MainPage):
                        lastPage = typeof(MainPage);
                        break;
                    case nameof(ComputerDevicesPage):
                        lastPage = typeof(ComputerDevicesPage);
                        break;
                    case nameof(ExplorerPage):
                        lastPage = typeof(ExplorerPage);
                        break;
                    case nameof(ExplorerHomePage):
                        lastPage = typeof(ExplorerHomePage);
                        break;
                    case nameof(PowerPage):
                        lastPage = typeof(PowerPage);
                        break;
                    case nameof(ControlPanelsPage):
                        lastPage = typeof(ControlPanelsPage);
                        break;
                    case nameof(HashGeneratorPage):
                        lastPage = typeof(HashGeneratorPage);
                        break;
                    default:
                        Trace.WriteLine("Unknown page");
                        break;
                }
            }
            catch (SettingNotFoundException) { }
        }

        public bool IsPaneOpen { get; set; }

        public void DisplayMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    ExceptionTextBlock.Text = message;
                    ExceptionPopup.IsOpen = true;
                });
            }
        }

        #region exception

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

        #endregion

        #region navigation events

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            if (lastPage != null)
            {
                _ = ContentFrame.Navigate(lastPage);
            }
            else
            {
                lastPage = typeof(MainPage);
                _ = ContentFrame.Navigate(typeof(MainPage));
            }
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                string tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "home":
                        lastPage = typeof(MainPage);
                        _ = ContentFrame.Navigate(typeof(MainPage));
                        break;
                    case "devices":
                        lastPage = typeof(ComputerDevicesPage);
                        _ = ContentFrame.Navigate(typeof(ComputerDevicesPage));
                        break;
                    case "explorer":
                        lastPage = typeof(ExplorerPage);
                        _ = ContentFrame.Navigate(typeof(ExplorerPage));
                        break;
                    case "explorerhome":
                        lastPage = typeof(ExplorerHomePage);
                        _ = ContentFrame.Navigate(typeof(ExplorerHomePage));
                        break;
                    case "power":
                        lastPage = typeof(PowerPage);
                        _ = ContentFrame.Navigate(typeof(PowerPage));
                        break;
                    case "controlpanels":
                        lastPage = typeof(ControlPanelsPage);
                        _ = ContentFrame.Navigate(typeof(ControlPanelsPage));
                        break;
                    case "hashgenerator":
                        lastPage = typeof(HashGeneratorPage);
                        _ = ContentFrame.Navigate(typeof(HashGeneratorPage));
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

        #endregion

        #region window events

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // save settings
            if (lastPage != null)
            Tool.SaveSetting(nameof(MainWindow), nameof(lastPage), lastPage.Name);
            Tool.SaveSetting(nameof(MainWindow), nameof(IsPaneOpen), IsPaneOpen);
        }

        #endregion
    }
}
