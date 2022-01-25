using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.DAL.Settings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorerSettingsPage : Page
    {
        private ISettingsManager settingsManager = App.Settings;

        public ExplorerSettingsPage()
        {
            InitializeComponent();
        }

        public bool LoadLastPath { get; set; }
        public bool KeepHistory { get; set; }
        public bool ClearHistoryButtonEnabled { get; set; }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
