using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.ControlPanels
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPanelsFilePage : Page
    {
        public ControlPanelsFilePage()
        {
            InitializeComponent();
        }

        public string Path { get; set; } = @"c:\users\julie\documents\multitool\custom settings.xml";

        public bool Valid { get; private set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Path))
            {
                if (File.Exists(Path))
                {
                    Valid = true;
                    _ = Frame.Navigate(typeof(ControlPanelsPage), Path);
                }
            }
        }
    }
}
