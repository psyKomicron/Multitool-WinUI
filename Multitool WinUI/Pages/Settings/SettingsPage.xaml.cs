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
using System.Diagnostics;
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
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        #region navigation events
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine(e.SourcePageType.Name);
            if (e.Parameter is string page)
            {
                // handle navigation to the wanted page
            }
            else if (e.Parameter is Type type)
            {
                string typeName = type.Name;
                Debug.WriteLine($"Settings: Navigating to {typeName}");
                if (!NavigateTo(typeName, true))
                {
                    App.TraceWarning($"Setting page not found for {typeName}");
                }
            }
            ISettingsManager settingsManager = App.Settings;
        }
        #endregion

        private bool NavigateTo(string tag, bool focus = false)
        {
            bool success;
            switch (tag)
            {
                case "ExplorerPage":
                    ContentFrame.Navigate(typeof(ExplorerSettingsPage), string.Empty);
                    success = true;
                    break;
                default:
                    success = false;
                    break;
            }
            if (success && focus)
            {
                var o = PagesNavigationView.Items.First((object item) =>
                {
                    return (((item as ListViewItem)?.Content as TextBlock)?.Tag as string) == tag;
                });
                PagesNavigationView.SelectedItem = o;
            }
            return success;
        }

        #region page event handlers
        private void PagesNavigationView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TextBlock block && block.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }
        #endregion
    }
}
