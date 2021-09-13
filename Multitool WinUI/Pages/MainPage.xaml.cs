using Microsoft.UI.Xaml.Controls;

using System;
using System.Reflection;
using System.Text;

using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Uri GithubUri = new(@"https://github.com/psyKomicron/multitool/tree/main");

        public MainPage()
        {
            InitializeComponent();
#if DEBUG
            BuildTypeBlock.Text = "Debug";
#else
            BuildTypeBlock.Text = "Release";
#endif
        }

#if DEBUG
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
#else
        public static string Version => GetPackageVersion();
#endif

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

        private void VersionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void ReadmeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void BuildTypeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
    }
}
