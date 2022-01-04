using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Helpers;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

using Windows.Storage;

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
                App.TraceError(e.ToString());
            }
            catch (IOException e)
            {
                App.TraceError(e.ToString());
            }
        }
    }
}
