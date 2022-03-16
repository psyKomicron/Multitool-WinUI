using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class WidgetSmallView : UserControl
    {
        public WidgetSmallView()
        {
            this.InitializeComponent();
        }

        public string WidgetName { get; set; }
        public string WidgetIcon { get; set; }

        public event RoutedEventHandler Clicked;

        public void SetSelected()
        {
            widgetFontIcon.Foreground = new SolidColorBrush(Tool.GetAppRessource<Color>("SystemAccentColor"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clicked?.Invoke(this, e);
        }
    }
}
