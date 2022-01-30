using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL.Settings;

using MultitoolWinUI.Controls;
using MultitoolWinUI.Helpers;
using MultitoolWinUI.Pages;
using MultitoolWinUI.Pages.ControlPanels;
using MultitoolWinUI.Pages.Explorer;
using MultitoolWinUI.Pages.HashGenerator;
using MultitoolWinUI.Pages.Power;
using MultitoolWinUI.Pages.Settings;
using MultitoolWinUI.Pages.Test;

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
        private bool closed;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Multitool v." + Tool.GetPackageVersion();
            SizeChanged += MainWindow_SizeChanged;
            try
            {
                App.Settings.Load(this);
            }
            catch
            {
                Trace.TraceWarning("Failed to load main window settings");
            }
        }

        [Setting(true)]
        public bool IsPaneOpen { get; set; }

        [Setting(typeof(TypeSettingConverter), HasDefaultValue = true, DefaultValue = typeof(MainPage))]
        public Type LastPage { get; set; }

        #region navigation events
        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            _ = ContentFrame.Navigate(LastPage);
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                string tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "home":
                        LastPage = typeof(MainPage);
                        _ = ContentFrame.Navigate(typeof(MainPage));
                        break;
                    case "devices":
                        LastPage = typeof(ComputerDevicesPage);
                        _ = ContentFrame.Navigate(typeof(ComputerDevicesPage));
                        break;
                    case "explorer":
                        LastPage = typeof(ExplorerPage);
                        _ = ContentFrame.Navigate(typeof(ExplorerPage));
                        break;
                    case "explorerhome":
                        LastPage = typeof(ExplorerHomePage);
                        _ = ContentFrame.Navigate(typeof(ExplorerHomePage));
                        break;
                    case "power":
                        LastPage = typeof(PowerPage);
                        _ = ContentFrame.Navigate(typeof(PowerPage));
                        break;
                    case "controlpanels":
                        LastPage = typeof(ControlPanelsPage);
                        _ = ContentFrame.Navigate(typeof(ControlPanelsPage));
                        break;
                    case "hashgenerator":
                        LastPage = typeof(HashGeneratorPage);
                        _ = ContentFrame.Navigate(typeof(HashGeneratorPage));
                        break;
                    case "irc":
                        LastPage = typeof(TwitchPage);
                        _ = ContentFrame.Navigate(typeof(TwitchPage));
                        break;
                    case "test":
                        LastPage = typeof(TestPage);
                        _ = ContentFrame.Navigate(typeof(TestPage));
                        break;
                    case "Settings":
                        _ = ContentFrame.Navigate(typeof(SettingsPage), LastPage);
                        break;
                    default:
                        App.TraceWarning("Trying to navigate to : " + tag);
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
        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _ = DispatcherQueue.TryEnqueue(() => MessageDisplay.Width = args.Size.Width);
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // save settings
            closed = true;
            MessageDisplay.Silence();
            try
            {
                App.Settings.Save(this);
            }
            catch (ArgumentException ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        private void MessageDisplay_VisibilityChanged(AppMessageControl sender, Visibility args)
        {
            if (!closed)
            {
                ContentPopup.IsOpen = args == Visibility.Visible;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {

        }
        #endregion
    }
}
