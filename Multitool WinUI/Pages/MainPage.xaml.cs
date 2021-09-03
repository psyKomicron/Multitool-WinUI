using Microsoft.UI.Xaml.Controls;

using System;
using System.Reflection;

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
        private readonly Uri GithubUri = new(@"https://github.com/psyKomicron/multitool/tree/main");

        public MainPage()
        {
            InitializeComponent();
        }

        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Border border)
            {
                if (border.Name == nameof(ReadmeBorder))
                {
                    _ = Launcher.LaunchUriAsync(GithubUri);
                }
            }
        }
    }
}
