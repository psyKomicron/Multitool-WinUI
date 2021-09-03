using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Controls;
using MultitoolWinUI.Pages.Explorer;

using System.IO;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorerHomePage : Page
    {
        private CancellationTokenSource token = new();

        public ExplorerHomePage()
        {
            InitializeComponent();
            Loaded += ExplorerHomePage_Loaded;
#if RELEASE
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo item in drives)
            {
                DisksGrid.Items.Add(new DriveInfoView(item, CancellationTokenSource.CreateLinkedTokenSource(token.Token))
                {
                    Margin = new Thickness(10),
                    Width = 550,
                    Height = 230,
                });
            }
#endif
        }

        private void ExplorerHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(ExplorerPage), @"C:\");
        }

        private void DisksGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            _ = Frame.Navigate(typeof(ExplorerPage), (e.ClickedItem as DriveInfoView).DriveInfo.Name);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            token.Cancel();
        }
    }
}
