using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Controls;

using System.IO;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Explorer
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
        }

        private void DisksGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            _ = Frame.Navigate(typeof(ExplorerPage), (e.ClickedItem as DriveInfoView).DriveInfo.Name);
            token.Cancel();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            token.Cancel();
        }
    }
}
