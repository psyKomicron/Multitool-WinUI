using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Helpers;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string settingPath = "main-page-settings.xml";
        private const string readMe = @"https://github.com/psyKomicron/Multitool-WinUI/blob/master/README.md";

        public MainPage()
        {
            InitializeComponent();
            InitializeWindow();
            BuildTypeBlock.Text = Tool.BuildType;
        }

        #region properties

#if DEBUG
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
#else
        public static string Version => Tool.GetPackageVersion();
#endif

        public string ReleaseNotes => Properties.Resources.ReleaseNotes;

        #endregion

        private void InitializeWindow()
        {
            XmlDocument doc = new();
            try
            {
                App.TraceInformation("Loading main page shortcuts");
                doc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, settingPath));
            }
            catch (XmlException e)
            {
                App.TraceError(e.Message);
            }
            catch (IOException e)
            {
                App.TraceError(e.Message);
            }
        }

        private void ReadmeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _ = Launcher.LaunchUriAsync(new(readMe));
        }
    }
}
