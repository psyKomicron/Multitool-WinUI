using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Net.Embeds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class VideoEmbedView : UserControl
    {
        private readonly VideoEmbed embed;
        private bool loaded;
        private ProgressRing downloadProgress;

        public VideoEmbedView(VideoEmbed embed)
        {
            this.embed = embed;
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        public string Description => embed.Description;
        public string Duration => embed.Duration.ToString(@"mm\:ss");
        public bool IsToolTipEnabled => !string.IsNullOrEmpty(embed.Description);
        public string Title => embed.Title;
        public Uri ThumbnailUrl => embed.ThumbnailUrl;
        public Uri Url => embed.Url;
        public string UploadDate => embed.UploadDate.ToShortDateString();

        private async Task OpenVideo()
        {
            try
            {
                if (embed.Url != null)
                {
                    await Launcher.LaunchUriAsync(embed.Url);
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Something went wrong went you clicked that link... Maybe try again ?");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!loaded && ThumbnailUrl != null)
            {
                downloadProgress = (ProgressRing)FindName("DownloadProgress");
                BitmapImage source = new()
                {
                    UriSource = ThumbnailUrl
                };
                source.DownloadProgress += ThumbnailDownloadProgress;
                Thumbnail.Source = source;
            }
            else if (ThumbnailUrl == null)
            {
                App.TraceWarning("Embed thumbnail is null");
            }
            loaded = true;
        }

        private void ThumbnailDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            if (downloadProgress.IsIndeterminate == true)
            {
                downloadProgress.IsIndeterminate = false;
            }
            downloadProgress.Value = e.Progress;

            if (e.Progress >= 100)
            {
                downloadProgress.Visibility = Visibility.Collapsed;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e) => await OpenVideo();
    }
}
