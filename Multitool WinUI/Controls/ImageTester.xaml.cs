using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

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

namespace MultitoolWinUI.Controls
{
    public sealed partial class ImageTester : UserControl
    {
        public ImageTester()
        {
            this.InitializeComponent();
        }

        private void SourceInputTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                Uri uri = new(SourceInputTextBox.Text);
                DownloadProgress.Visibility = Visibility.Visible;
                DownloadProgress.IsIndeterminate = true;
                BitmapImage source = new(uri);
                DownloadProgress.IsIndeterminate = false;
                source.DownloadProgress += Source_DownloadProgress;
                TestImage.Source = source;
            }
        }

        private void Source_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            if (DownloadProgress.IsIndeterminate == true)
            {
                DownloadProgress.IsIndeterminate = false;
            }

            DownloadProgress.Value = e.Progress;
        }
    }
}
