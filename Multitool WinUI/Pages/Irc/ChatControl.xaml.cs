using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Imaging;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;
using Multitool.Net.Twitch.Security;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Models;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.UI;

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
        private readonly SolidColorBrush messageBackground = new(Colors.MediumPurple);
        private readonly SolidColorBrush timestampBrush = new(Colors.White) { Opacity = 0.5 };
        private readonly ConcurrentDictionary<Color, SolidColorBrush> messageColors = new();
        private bool joined;
        private bool loaded;

        public ChatControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        #region properties
        public ObservableCollection<MessageModel> Chat { get; set; } = new();

        public List<Emote> Emotes { get; set; } = new();

        public List<Emote> ChannelEmotes { get; set; } = new();

        public string Channel { get; set; }

        public int MaxMessages { get; set; } = 500;

        public TabViewItem Tab { get; set; }

        public ITwitchIrcClient Client { get; set; }

        public double EmoteSize { get; set; } = 20;

        public string TimestampFormat { get; set; } = "T";
        #endregion

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
        }

        #region private methods

        private async Task Join()
        {
            if (!joined && Client != null && !string.IsNullOrEmpty(Channel))
            {
                try
                {
                    ChannelEmotes.AddRange(await TwitchEmoteProxy.GetInstance().GetChannelEmotes(Channel));
                    await Client.Join(Channel);
                    joined = true;
                    RoomStateDisplay.QueueMessage("Channel", "Joined " + Channel, messageBackground);
                }
                catch (ArgumentException ex)
                {
                    App.TraceError(ex.ToString());

                    Message message = new(ex.Message)
                    {
                        Author = thisUser
                    };
                    Chat.Add(new(message));
                }
                catch (InvalidOperationException ex)
                {
                    App.TraceError(ex.ToString());

                    Message message = new($"Cannot connect: {ex.Message}")
                    {
                        Author = thisUser
                    };
                    Chat.Add(new(message));
                }
                catch (Exception ex)
                {
                    App.TraceError(ex.ToString());
                }
            }
        }

        private RichTextBlock CreateMessage(Message message)
        {
            RichTextBlock presenter = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true
            };

            Paragraph paragraph = new()
            {
                LineHeight = 24,
                CharacterSpacing = 15,
                LineStackingStrategy = LineStackingStrategy.MaxHeight
            };

            // timestamp
            paragraph.Inlines.Add(new Run()
            {
                Text = message.ServerTimestamp.ToString(TimestampFormat) + " ",
                Foreground = timestampBrush,
                FontWeight = FontWeights.Light
            });
            // name
            paragraph.Inlines.Add(new Run()
            {
                Text = (string.IsNullOrEmpty(message.Author.DisplayName) ? message.Author.Name : message.Author.DisplayName) + ": ",
                Foreground = messageColors.GetOrAdd(message.Author.NameColor, GetOrCreate),
                FontWeight = FontWeights.Bold
            });

            string[] words = message.ToString().Split(' ');
            bool text = true;
            for (int i = 0; i < words.Length; i++)
            {
                if (Tool.IsRelativeUrl(words[i]))
                {
                    text = PutHyperlink(paragraph, words[i]);
                    break;
                }
                else if (Emotes != null)
                {
                    for (int j = 0; j < Emotes.Count; j++)
                    {
                        if (Emotes[j].Name.Equals(words[i]))
                        {
                            PutImage(paragraph, Emotes[j]);
                            text = false;
                            break;
                        }
                    }
                }

                if (text)
                {
                    Run run = new()
                    {
                        Text = words[i] + ' '
                    };
                    if (words[i].Length > 0)
                    {
                        run.FontWeight = words[i][0] == '@' ? FontWeights.Bold : FontWeights.Normal;
                    }
                    paragraph.Inlines.Add(run);
                }
                text = true;
            }

            presenter.Blocks.Add(paragraph);
            return presenter;
        }

        private static bool PutHyperlink(Paragraph paragraph, string text)
        {
            try
            {
                Hyperlink container = new()
                {
                    NavigateUri = new(text)
                };
                container.Inlines.Add(new Run()
                {
                    Text = text
                });

                paragraph.Inlines.Add(container);
                return false;
            }
            catch (UriFormatException ex)
            {
                App.TraceError(ex.Message + "\n" + text);
                return true;
            }
        }

        private void PutImage(Paragraph paragraph, Emote emote)
        {
            InlineUIContainer imageContainer = new()
            {
                Child = new Image()
                {
                    Source = emote.Image,
                    Height = EmoteSize,
                    Margin = new(0, 0, 0, -(EmoteSize / 2)),
                    ContextFlyout = new Flyout()
                    {
                        Content = new TextBlock() { Text = $"Provider {emote.Provider}" }
                    }
                }
            };
            paragraph.Inlines.Add(imageContainer);
        }

        private SolidColorBrush GetOrCreate(Color p)
        {
            return new(p);
        }

        #endregion

        #region event handlers

        #region irc events

        private void Client_RoomChanged(ITwitchIrcClient sender, RoomStates args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args)
                {
                    case RoomStates.EmoteOnlyOn:
                        RoomStateDisplay.QueueMessage("Room change", "Emote only on", messageBackground);
                        break;
                    case RoomStates.EmoteOnlyOff:
                        RoomStateDisplay.QueueMessage("Room change", "Emote only off", messageBackground);
                        break;

                    case RoomStates.FollowersOnlyOn:
                        RoomStateDisplay.QueueMessage("Room change", "Followers only on", messageBackground);
                        break;
                    case RoomStates.FollowersOnlyOff:
                        RoomStateDisplay.QueueMessage("Room change", "Followers only off", messageBackground);
                        break;

                    case RoomStates.R9KOn:
                        RoomStateDisplay.QueueMessage("Room change", "R9K on", messageBackground);
                        break;
                    case RoomStates.R9KOff:
                        RoomStateDisplay.QueueMessage("Room change", "R9K off", messageBackground);
                        break;

                    case RoomStates.SlowModeOn:
                        RoomStateDisplay.QueueMessage("Room change", "Slow mode on", messageBackground);
                        break;
                    case RoomStates.SlowModeOff:
                        RoomStateDisplay.QueueMessage("Room change", "Slow mode off", messageBackground);
                        break;

                    default:
                        break;
                }
            });
        }

        private void Client_Disconnected(ITwitchIrcClient sender, EventArgs args)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                    {
                        RoomStateDisplay.QueueMessage("Warning", "The client has been disconnected from the channel", messageBackground);
                    });
            }
        }

        private void Client_MessageReceived(ITwitchIrcClient sender, Message args)
        {
            if (DispatcherQueue == null) return;

            DispatcherQueue?.TryEnqueue(() =>
            {
                MessageModel model = new()
                {
                    Message = CreateMessage(args),
                };

                Chat.Add(model);
                NumberOfMessages_TextBlock.Text = Chat.Count.ToString();
            });

            if (Chat.Count > MaxMessages)
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        if (DispatcherQueue == null)
                        {
                            return;
                        }
                        try
                        {
                            Chat.RemoveAt(i);
                        }
                        catch { return; }
                    }
                });
            }
        }

        #endregion

        #region ui events

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;

            TextBox header = new()
            {
                PlaceholderText = Channel ?? "Select channel...",
                BorderThickness = new(0),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Tab.Header = header;

            Tab.CloseRequested += Tab_CloseRequested;
            header.KeyDown += Header_KeyDown;

            Client.MessageReceived += Client_MessageReceived;
            Client.Disconnected += Client_Disconnected;
            Client.RoomChanged += Client_RoomChanged;

            if (!string.IsNullOrEmpty(Channel))
            {
                _ = Join();
            }

            Emotes.AddRange(await TwitchEmoteProxy.GetInstance().GetGlobalEmotes());

            loaded = true;
        }

        private async void Tab_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
        {
            loaded = false;
            try
            {
                await Client.Disconnect();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            try
            {
                await Client.DisposeAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            Chat.Clear();
            Chat = null;
            UnloadObject(Chat_ListView);
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
            loaded = false;
            if (Client != null && Client.IsConnected)
            {
                Client.MessageReceived -= Client_MessageReceived;
                Client.RoomChanged -= Client_RoomChanged;
                try
                {
                    await Client.Disconnect();
                }
                catch (InvalidOperationException) { }
                await Client.DisposeAsync();
            }
        }

        private void MessageDisplay_VisibilityChanged(Controls.AppMessageControl sender, Visibility args)
        {
            if (loaded) UpdatePopup.IsOpen = args == Visibility.Visible;
        }

        private void EmoteGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Debug.WriteLine($"Clicked {(e.ClickedItem as Emote).Name}");
            ChatInput.Text += $" {(e.ClickedItem as Emote)} ";
        }
        #endregion

        #endregion
    }
}
