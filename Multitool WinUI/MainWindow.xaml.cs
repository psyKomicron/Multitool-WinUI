using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Multitool.DAL.Settings;
using Multitool.DAL.Settings.Converters;
using Multitool.NTInterop;

using MultitoolWinUI.Controls;
using MultitoolWinUI.Helpers;
using MultitoolWinUI.Pages;
using MultitoolWinUI.Pages.ControlPanels;
using MultitoolWinUI.Pages.Explorer;
using MultitoolWinUI.Pages.HashGenerator;
using MultitoolWinUI.Pages.MusicPlayer;
using MultitoolWinUI.Pages.Power;
using MultitoolWinUI.Pages.Settings;
using MultitoolWinUI.Pages.Test;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;

using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static AppWindow thisWindow;
        private bool closed;

        public MainWindow()
        {
            InitializeComponent();
            SetTitleBar();
            SizeChanged += MainWindow_SizeChanged;
            try
            {
                App.Settings.Load(this);
                InteropWrapper.SetWindowSize(this, WindowSize, new(PositionY, PositionX));
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

        [Setting(typeof(SizeSettingConverter), 1000, 600)]
        public Size WindowSize { get; set; }

        [Setting(0)]
        public int PositionX { get; set; }

        [Setting(0)]
        public int PositionY { get; set; }

        public int TitleBarHeight { get; private set; }

        #region navigation events
        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = ContentFrame.Navigate(LastPage);
            }
            catch (Exception ex)
            {
                App.TraceError(ex);
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
                    case "music":
                        LastPage = typeof(MusicPlayerPage);
                        _ = ContentFrame.Navigate(typeof(MusicPlayerPage));
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

        private void SetTitleBar()
        {
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            thisWindow = AppWindow.GetFromWindowId(windowId);
            thisWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            thisWindow.TitleBar.ButtonBackgroundColor = Tool.GetAppRessource<Color>("DarkBlack");
            thisWindow.TitleBar.ButtonForegroundColor = Colors.White;
            thisWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            thisWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            thisWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            thisWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
            thisWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            thisWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;

            string fileName = "peepoPoopoo.gif";
            //Directory.GetFiles("/Resources/Images");
            Uri imageSource = new(@$"ms-appx:///Resources/Images/{fileName}");
            WindowIcon.Source = new BitmapImage(imageSource);
        }

        /*private void SetDragRegionForCustomTitleBar()
        {
            //Infer titlebar height
            int titleBarHeight = 32;

            // Get caption button occlusion information
            // Use LeftInset if you've explicitly set your window layout to RTL or if app language is a RTL language
            int CaptionButtonOcclusionWidth = thisWindow.TitleBar.RightInset;

            // Define your drag Regions
            int windowIconWidthAndPadding = (int)(PresenterModeButton.Width + WindowIcon.Width + WindowTitleTextBlock.Width + 10);
            int dragRegionWidth = thisWindow.Size.Width - (CaptionButtonOcclusionWidth + windowIconWidthAndPadding);

            RectInt32[] dragRects = new RectInt32[] { };
            RectInt32 dragRect;

            dragRect.X = windowIconWidthAndPadding;
            dragRect.Y = 0;
            dragRect.Height = titleBarHeight;
            dragRect.Width = dragRegionWidth;

            thisWindow.TitleBar.SetDragRectangles(dragRects.Append(dragRect).ToArray());
        }*/

        #region window events
        private void PresenterModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (thisWindow.Presenter.Kind != AppWindowPresenterKind.CompactOverlay)
            {
                thisWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            }
            else
            {
                thisWindow.SetPresenter(AppWindowPresenterKind.Default);
            }
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            if (!closed)
            {
                WindowSize = args.Size;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // save settings
            closed = true;
            MessageDisplay.Silence();
            try
            {
                PositionX = thisWindow.Position.X;
                PositionY = thisWindow.Position.Y;
                App.Settings.Save(this);
            }
            catch (ArgumentException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch { }
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
