using Microsoft.UI.Xaml.Controls;

using Multitool.Data.Settings;

using MultitoolWinUI.Helpers;

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
        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
#else
        public string Version => Tool.GetPackageVersion();
#endif

        public string ReleaseNotes => Properties.Resources.ReleaseNotes;

        [Setting(true)]
        public bool LoadShortcuts { get; set; }
        #endregion

        private void InitializeWindow()
        {
            App.Settings.Load(this);
            if (LoadShortcuts)
            {
                XmlDocument doc = new();
                try
                {
                    App.TraceInformation("Loading main page shortcuts");
                    doc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, settingPath));
                }
                catch (XmlException e)
                {
                    App.TraceError(e);
                }
                catch (IOException e)
                {
                    App.TraceError(e);
                }
            }
        }

        private void ReadmeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _ = Launcher.LaunchUriAsync(new(readMe));
        }
    }
}
