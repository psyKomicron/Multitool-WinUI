using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Net.Twitch;
using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Irc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page, IAsyncDisposable
    {
        private const string wssUri = @"wss://irc-ws.chat.twitch.tv:443";
        private readonly User thisUser = User.CreateSystemUser();
        private IIrcClient client;
        private TabViewItem tab;
        private bool joined;
        private bool loaded;

        public ChatPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;

            Uri uri = new(@"C:\Users\julie\Documents\GitHub\Multitool-WinUI\Multitool WinUI\Resources\Images\catJAM-1x.gif", UriKind.Absolute);
            var source = new BitmapImage(uri);
            Chat.Add(new()
            {
                UserName = "test",
                Message = new Image()
                {
                    Source = source,
                    Height = 20,
                    Width = 20
                }
            });
        }

        public ObservableCollection<ChatMessageModel> Chat { get; } = new();

        public string Channel { get; set; }

        public int MaxMessages { get; set; } = 500;

        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
        }

        private void Load(IIrcClient client, TabViewItem tab)
        {
            this.client = client;
            this.tab = tab;
            /*TextBox header = new()
            {
                PlaceholderText = "Channel (none)",
                BorderThickness = new(0)
            };
            this.tab.Header = header;*/

            this.tab.CloseRequested += Tab_CloseRequested;
            //header.KeyDown += Header_KeyDown;
            this.client.MessageReceived += OnMessageReceived;
        }

        private async Task Join()
        {
            if (!joined && client != null && !string.IsNullOrEmpty(Channel))
            {
                try
                {
                    if (!client.Connected)
                    {
                        await client.Connect(new(wssUri));
                    }
                    await client.Join(Channel);
                    joined = true;
                }
                catch (ArgumentException ex)
                {
                    Trace.TraceError(ex.ToString());

                    Message message = new(ex.Message)
                    {
                        Author = thisUser
                    };
                    Chat.Add(new(message));
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceError(ex.ToString());

                    Message message = new($"Cannot connect: {ex.Message}")
                    {
                        Author = thisUser
                    };
                    Chat.Add(new(message));
                }
            }
        }

        #region event handlers
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ChatPageParameter param)
            {
                Channel = param.Channel;
                Load(param.Client, param.Tab);
            }
        }

        private void OnMessageReceived(IIrcClient sender, Message args)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() => Chat.Add(new(args)));
                if (Chat.Count > MaxMessages)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            Chat.RemoveAt(i);
                        }
                    });
                }
            }
        }

        private async void Tab_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
        {
            Chat.Clear();
            await client.DisposeAsync();
            loaded = false;
        }

        private void Header_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && sender is TextBox box)
            {
                Channel = box.Text;
                if (loaded)
                {
                    _ = Join();
                }
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                await Join();
                loaded = true;
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (client != null && client.Connected)
            {
                try
                {
                    await client.Part(Channel);
                }
                catch (InvalidOperationException) { }
                await client.DisposeAsync();
            }
        }
        #endregion
    }
}
