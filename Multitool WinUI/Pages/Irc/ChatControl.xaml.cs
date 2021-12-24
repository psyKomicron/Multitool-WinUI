using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Irc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatControl : UserControl, IAsyncDisposable
    {
        private readonly User thisUser = User.CreateSystemUser();
        private bool joined;
        private bool loaded;

        public ChatControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        public ObservableCollection<ChatMessageModel> Chat { get; } = new();

        public List<Emote> Emotes { get; set; }

        public string Channel { get; set; }

        public int MaxMessages { get; set; } = 500;

        public TabViewItem Tab { get; set; }

        public ITwitchIrcClient Client { get; set; }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
        }

        private async Task Join()
        {
            if (!joined && Client != null && !string.IsNullOrEmpty(Channel))
            {
                try
                {
                    if (!Client.IsConnected)
                    {
                        await Client.Connect();
                    }
                    await Client.Join(Channel);
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
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        #region event handlers
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            TextBox header = new()
            {
                PlaceholderText = Channel ?? "Select channel...",
                BorderThickness = new(0)
            };
            Tab.Header = header;

            Tab.CloseRequested += Tab_CloseRequested;
            header.KeyDown += Header_KeyDown;
            Client.MessageReceived += OnMessageReceived;
            Client.Connected += Client_Connected;
            Client.Disconnected += Client_Disconnected;

            if (!string.IsNullOrEmpty(Channel))
            {
                _ = Join();
            }
            loaded = true;
        }

        private void Client_Disconnected(ITwitchIrcClient sender, EventArgs args)
        {
            Trace.TraceInformation($"Disconnected from {Channel}");
        }

        private void Client_Connected(ITwitchIrcClient sender, EventArgs args)
        {
            Trace.TraceInformation($"Connected to {Channel}");
        }

        private void OnMessageReceived(ITwitchIrcClient sender, Message args)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() => 
                {
                    if (DispatcherQueue != null)
                    {
                        RichTextBlock presenter = new()
                        {
                            TextWrapping = TextWrapping.WrapWholeWords,
                            IsTextSelectionEnabled = true,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        Paragraph paragraph = new();
                        string[] words = args.ToString().Split(' ');
                        bool text = true;
                        for (int i = 0; i < words.Length; i++)
                        {
                            for (int j = 0; j < Emotes.Count; j++)
                            {
                                if (Emotes[j].NameRegex.IsMatch(words[i]))
                                {
                                    InlineUIContainer imageContainer = new()
                                    {
                                        Child = new Image()
                                        {
                                            Source = Emotes[j].Image,
                                            Height = 30
                                        }
                                    };
                                    paragraph.Inlines.Add(imageContainer);

                                    text = false;
                                    break;
                                }
                            }

                            if (text)
                            {
                                paragraph.Inlines.Add(new Run()
                                {
                                    Text = words[i] + ' '
                                });
                            }
                        }
                        presenter.Blocks.Add(paragraph);
                        ChatMessageModel model = new()
                        {
                            Message = presenter,
                            Timestamp = args.ServerTimestamp.ToString("t"),
                            UserName = string.IsNullOrEmpty(args.Author.DisplayName) ? args.Author.Name : args.Author.DisplayName,
                            NameColor = new(args.Author.NameColor)
                        };

                        Chat.Add(model);
                    }
                });

                if (Chat.Count > MaxMessages)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            if (DispatcherQueue == null)
                            {
                                return;
                            }
                            Chat.RemoveAt(i);
                        }
                    });
                }
            }
        }

        private async void Tab_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
        {
            Chat.Clear();
            await Client.DisposeAsync();
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

                Tab.Header = new TextBlock()
                {
                    Text = Channel
                };
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Client != null && Client.IsConnected)
            {
                try
                {
                    await Client.Part(Channel);
                }
                catch (InvalidOperationException) { }
                await Client.DisposeAsync();
            }
        }
        #endregion
    }
}
