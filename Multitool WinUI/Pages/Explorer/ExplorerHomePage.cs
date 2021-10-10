using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Controls;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Explorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorerHomePage : Page
    {
        private readonly CancellationTokenSource token = new();

        public ExplorerHomePage()
        {
            InitializeComponent();
            Loaded += ExplorerHomePage_Loaded;
        }

        private void ExplorerHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo item in drives)
            {
                DriveInfoView view = new(item, CancellationTokenSource.CreateLinkedTokenSource(token.Token))
                {
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    CornerRadius = new(5)
                };
                DisksGrid.Items.Add(view);
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
