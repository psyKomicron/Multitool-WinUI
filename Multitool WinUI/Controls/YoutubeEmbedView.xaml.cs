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
using System.Diagnostics;
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
    public sealed partial class YoutubeEmbedView : UserControl
    {
        private ProgressRing downloadProgress;
        private bool loaded;

        public YoutubeEmbedView(YoutubeEmbed embed)
        {
            this.InitializeComponent();
            Embed = embed;
            Loaded += OnLoaded;
        }

        public YoutubeEmbed Embed { get; }

        public string Author => Embed.Author;
        public string ChannelId => Embed.ChannelId;
        public DateTime PublishDate => Embed.PublishDate;
        public DateTime UploadDate => Embed.UploadDate;
        public string Description => Embed.Description;
        public string Duration => Embed.Duration.ToString(@"mm\:ss");
        public bool FamilyFriendly => Embed.FamilyFriendly;
        public string Genre => Embed.Genre;
        public long Interactions => Embed.Interactions;
        public string Title => Embed.Title;
        public string Paid => Embed.Paid ? "Paid" : string.Empty;
        public string Regions => Embed.Regions.ToString();
        public Uri ThumbnailUrl => Embed.ThumbnailUrl;
        public Uri EmbedUrl => Embed.EmbedUrl;
        public Uri Url => Embed.Url;
        public string Unlisted => Embed.Unlisted ? "Unlisted" : "Public";
        public string VideoId => Embed.VideoId;

        private async Task OpenVideo()
        {
            try
            {
                if (Embed.Url != null)
                {
                    await Launcher.LaunchUriAsync(Embed.Url);
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
                BitmapImage source = new()
                {
                    UriSource = ThumbnailUrl
                };
                source.DownloadProgress += ThumbnailDownloadProgress;
                Thumbnail.Source = source;
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

        private async void AuthorHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new($"https://youtube.com/channel/{Embed.ChannelId}"));
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Cannot open channel link... Maybe try again ?");
            }
        }
    }
}
