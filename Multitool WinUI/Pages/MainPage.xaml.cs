using Microsoft.UI.Xaml.Controls;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Windows.ApplicationModel;
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
#if DEBUG
            BuildTypeBlock.Text = "Debug";
#else
            BuildTypeBlock.Text = "Release";
#endif
        }

        #region properties

#if DEBUG
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
#else
        public static string Version => GetPackageVersion();
#endif

        #endregion

        private void InitializeWindow()
        {
            XmlDocument doc = new();
            try
            {
                doc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, settingPath));
                Trace.TraceInformation("Loading main page shortcuts");
            }
            catch (XmlException e)
            {
                Trace.TraceError(e.ToString());
            }
            catch (IOException e)
            {
                Trace.TraceError(e.ToString());
            }
        }

#if !DEBUG
        private static string GetPackageVersion()
        {
            StringBuilder builder = new();
            builder.Append(Package.Current.Id.Version.Major)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Minor)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Build)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Revision);
            return builder.ToString();
        }
#endif
    }
}
