using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Widgets
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetSelectedPage : Page
    {
        public WidgetSelectedPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] array)
            {
                if (array.Length > 0 && array[0] is WidgetsPageNavigationInfo navigationInfo)
                {
                    widgetFontIcon.Glyph = navigationInfo.Widget.WidgetIcon;
                    widgetTitle.Text = navigationInfo.Widget.WidgetName;
                    SetControl(navigationInfo.Control);
                } 
            }
        }

        private void SetControl(Control control)
        {
            controlGrid.Children.Clear();
            Grid.SetColumn(control, 0);
            Grid.SetRow(control, 1);
            controlGrid.Children.Add(control);
        }
    }
}
