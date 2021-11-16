using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Irc;

using MultitoolWinUI.Models;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class ChatControl : UserControl, IDisposable
    {
        private const string wssUri = @"wss://irc-ws.chat.twitch.tv:443";
        private readonly IIrcClient client;
        private readonly TabViewItem tab;

        public ChatControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ChatControl(IIrcClient client, TabViewItem tab) : this()
        {
            this.client = client;
            this.tab = tab;
            TextBox header = new()
            {
                PlaceholderText = "Channel (none)"
            };
            this.tab.Header = header;
            this.client.Connect(new(wssUri));

            this.tab.CloseRequested += Tab_CloseRequested;
            header.KeyDown += Header_KeyDown;
            header.TextChanged += Header_TextChanged;
            this.client.MessageReceived += OnMessageReceived;
        }

        private void Tab_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (client.Connected)
            {
                client.Disconnect();
            }
        }

        public ObservableCollection<ChatMessageModel> Chat { get; } = new();

        public string Channel { get; set; }

        public void Dispose()
        {
            client.Dispose();
        }

        #region event handlers
        private void Header_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box && box.Text.EndsWith('\n'))
            {
                try
                {
                    client.Join($"#{box.Text[0..^2]}");
                }
                catch (ArgumentException ex)
                {
                    Trace.TraceError(ex.ToString());
                    Chat.Add(new("system", ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceError(ex.ToString());
                    Chat.Add(new("system", "Cannot connect to twitch"));
                }
            }
        }

        private async void Header_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && sender is TextBox box)
            {
                try
                {
                    await client.Join($"{box.Text}");
                }
                catch (ArgumentException ex)
                {
                    Trace.TraceError(ex.ToString());
                    Chat.Add(new("system", ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceError(ex.ToString());
                    Chat.Add(new("system", "Cannot connect to twitch"));
                }
            }
        }

        private void OnMessageReceived(IIrcClient sender, string args)
        {
            DispatcherQueue.TryEnqueue(() => Chat.Add(new(string.Empty, args)));
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            /*try
            {
                if (client != null && client.ClientState == System.Net.WebSockets.WebSocketState.Open)
                {
                    await client.Part(LastStream);
                    await client.Join(uri);
                }
            }
            catch (ArgumentException ex)
            {
                Trace.TraceError(ex.ToString());
            }*/

            if (client != null)
            {
                await client.Connect(new(wssUri));
                if (!string.IsNullOrEmpty(Channel))
                {
                    await client.Join(Channel);
                }
            }
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.MessageReceived -= OnMessageReceived;
                if (client.Connected)
                {
                    await client.Part(Channel);
                }
                await client.Disconnect();
            }
        }
        #endregion
    }
}
