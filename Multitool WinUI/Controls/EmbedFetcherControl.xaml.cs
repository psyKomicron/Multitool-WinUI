using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Multitool.Collections;
using Multitool.Net.Embeds;

using MultitoolWinUI.Controls;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class EmbedFetcherControl : UserControl
    {
        private readonly DelayedActionQueue queue = new(1500);

        public EmbedFetcherControl()
        {
            this.InitializeComponent();
            queue.DispatcherQueue = DispatcherQueue;
            queue.QueueEmpty += Queue_QueueEmpty;

            Fetchers = new();
            Fetchers.Add(new YoutubeEmbedFetcher());
            Fetchers.Add(new TwitchEmbedFetcher());
        }

        public List<IEmbedFetcher> Fetchers { get; }
        public string LinkInput { get; set; }

        private async Task FetchEmbeds()
        {
            foreach (var fetcher in Fetchers)
            {
                if (fetcher.CanFetch(LinkInput))
                {
                    queue.QueueAction(() =>
                    {
                        ControlInfoBar.Severity = InfoBarSeverity.Informational;
                        ControlInfoBar.Message = "Fetching embed";
                        ControlInfoBar.IsOpen = true;
                    });

                    try
                    {
                        var embed = await fetcher.Fetch(new(LinkInput));
                        queue.QueueAction(() =>
                        {
                            ControlInfoBar.Severity = InfoBarSeverity.Success;
                            ControlInfoBar.Message = "Fetched embed";
                            ControlInfoBar.IsOpen = true;
                        });

                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (embed is YoutubeEmbed youtubeEmbed)
                            {
                                EmbedListView.Items.Add(new YoutubeEmbedView(youtubeEmbed)
                                {
                                    HorizontalAlignment = HorizontalAlignment.Left
                                });
                            }
                            else if (embed is VideoEmbed videoEmbed)
                            {
                                EmbedListView.Items.Add(new VideoEmbedView(videoEmbed)
                                {
                                    HorizontalAlignment = HorizontalAlignment.Left
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        queue.QueueAction(() =>
                        {
                            ControlInfoBar.Severity = InfoBarSeverity.Error;
                            ControlInfoBar.Message = $"Error while fetching embed:\n{ex.Message}";
                            ControlInfoBar.IsOpen = true;
                        });
                    }
                }
            }
        }

        private void Queue_QueueEmpty(DelayedActionQueue sender, System.Timers.ElapsedEventArgs args)
        {
            ControlInfoBar.IsOpen = false;
        }

        private void FetchButton_Click(object sender, RoutedEventArgs e) => _ = FetchEmbeds();

        private void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                _ = FetchEmbeds();
            }
        }
    }
}
