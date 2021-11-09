using Microsoft.UI.Xaml;

using Multitool.DAL;

using System.Diagnostics;

using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            UnhandledException += OnUnhandledException;
            InitializeComponent();
        }

        public static MainWindow MainWindow { get; private set; }

        public static ISettings Settings { get; private set; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Trace.TraceInformation("Application starting. Listing saved settings : ");
            Settings = new Settings(ApplicationData.Current.LocalSettings)
            {
                SettingFormat = "{0}/{1}"
            };
            var settings = Settings.GetAllSettings();
            foreach (var item in settings)
            {
                Trace.TraceInformation($"\t{item.Key} : {item.Value}");
            }
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError("App -> Unhandled exception: " + e.Exception.ToString());
        }
    }
}
