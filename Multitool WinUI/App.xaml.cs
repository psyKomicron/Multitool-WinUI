using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Multitool.DAL;
using Multitool.DAL.Settings;

using MultitoolWinUI.Helpers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Windows.Storage;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static SolidColorBrush errorBrush;
        private static SolidColorBrush warningBrush;
        private static SolidColorBrush infoBrush;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        public static MainWindow MainWindow { get; private set; }

        public static ISettingsManager Settings { get; private set; }

        public static void TraceInformation(string info)
        {
            TraceMessage("Information", info, infoBrush);
        }

        public static void TraceWarning(string warning)
        {
            TraceMessage("Warning", warning, warningBrush);
        }

        public static void TraceError(string error)
        {
            TraceMessage("Error", error, errorBrush);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Trace.TraceInformation("Application starting...");
#if DEBUG
            Test(); 
#endif
            Settings = new SettingsManager(ApplicationData.Current.LocalSettings)
            {
                SettingFormat = "{0}/{1}"
            };

            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);

            try
            {
                errorBrush = new(Tool.GetAppRessource<Color>("SystemAccentColor"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }
            try
            {
                warningBrush = new(Tool.GetAppRessource<Color>("DevOrange"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }
            try
            {
                infoBrush = new(Tool.GetAppRessource<Color>("DevBlue"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }


            MainWindow = new MainWindow();
            Trace.Listeners.Add(new WindowTrace(MainWindow));
            MainWindow.Activate();
        }

        private static void TraceMessage(string title, string message, Brush background)
        {
            if (MainWindow != null)
            {
                MainWindow.MessageDisplay.QueueMessage(title, message, background);
            }
        }

#if DEBUG
        private async void Test()
        {
            try
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("settings.xml", CreationCollisionOption.OpenIfExists);
                var settingManager = new XmlSettingManager();
                settingManager.Save(new Test());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        } 
#endif
    }

#if DEBUG
    class Test
    {
        public Test()
        {
            String = "flkdsnfeifslkn";
            Number = 1298;
            List = new List<int> { 1, 2, 4, 8, 213, 234, 234235, 23, 12, 667, 43358 };
        }

        [Setting]
        public string String { get; set; }
        [Setting]
        public int Number { get; set; }
        [Setting]
        public List<int> List { get; set; }
    } 
#endif
}
