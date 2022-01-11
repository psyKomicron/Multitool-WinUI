using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

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
        private readonly SolidColorBrush messageForeground = new(Colors.MediumPurple);
        private bool joined;
        private bool loaded;

        public ChatControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        #region properties
        public ObservableCollection<ChatMessageModel> Chat { get; set; } = new();

        public List<Emote> Emotes { get; set; }

        public string Channel { get; set; }

        public int MaxMessages { get; set; } = 500;

        public TabViewItem Tab { get; set; }

        public ITwitchIrcClient Client { get; set; }

        public int EmoteSize { get; set; } = 20;
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
                    await Client.Join(Channel);
                    joined = true;
                    App.TraceInformation("Joined " + Channel);
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
                IsTextSelectionEnabled = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Paragraph paragraph = new();
            string[] words = message.ToString().Split(' ');
            bool text = true;
            for (int i = 0; i < words.Length; i++)
            {
                if (Tool.IsRelativeUrl(words[i]))
                {
                    try
                    {
                        Hyperlink container = new()
                        {
                            NavigateUri = new(words[i])
                        };
                        container.Inlines.Add(new Run()
                        {
                            Text = words[i]
                        });

                        paragraph.Inlines.Add(container);
                        text = false;
                    }
                    catch (UriFormatException ex)
                    {
                        App.TraceError(ex.Message + "\n" + words[i]);
                    }
                    break;
                }
                else if (Emotes != null)
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
                                    Height = EmoteSize
                                }
                            };
                            paragraph.Inlines.Add(imageContainer);

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
        #endregion

        #region event handlers
        private void Client_RoomChanged(ITwitchIrcClient sender, RoomStates args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args)
                {
                    case RoomStates.EmoteOnlyOn:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Emote only on", messageBackground);
                        break;
                    case RoomStates.EmoteOnlyOff:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Emote only off", messageBackground);
                        break;

                    case RoomStates.FollowersOnlyOn:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Followers only on", messageBackground);
                        break;
                    case RoomStates.FollowersOnlyOff:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Followers only off", messageBackground);
                        break;

                    case RoomStates.R9KOn:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "R9K on", messageBackground);
                        break;
                    case RoomStates.R9KOff:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "R9K off", messageBackground);
                        break;

                    case RoomStates.SlowModeOn:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Slow mode on", messageBackground);
                        break;
                    case RoomStates.SlowModeOff:
                        RoomStateDisplay.QueueMessage(string.Empty, "Room change", "Slow mode off", messageBackground);
                        break;

                    default:
                        break;
                }
            });
        }

        private void Client_Disconnected(ITwitchIrcClient sender, EventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RoomStateDisplay.QueueMessage("Information", "Disconnected", "The client has been disconnected from the channel", messageBackground);
            });
        }

        private void Client_MessageReceived(ITwitchIrcClient sender, Message args)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() => 
                {
                    ChatMessageModel model = new()
                    {
                        Message = CreateMessage(args),
                        Timestamp = args.ServerTimestamp.ToString("t"),
                        UserName = string.IsNullOrEmpty(args.Author.DisplayName) ? args.Author.Name : args.Author.DisplayName,
                        NameColor = /*new(args.Author.NameColor)*/messageForeground
                    };

                    Chat.Add(model);
                    NumberOfMessages_TextBlock.Text = Chat.Count.ToString();
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
                            try
                            {
                                Chat.RemoveAt(i);
                            }
                            catch { return; }
                        }
                    });
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
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
            if (Client != null && Client.IsConnected)
            {
                Client.MessageReceived -= Client_MessageReceived;
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
            UpdatePopup.IsOpen = args == Visibility.Visible;
        }

        private void ChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            ContentGrid.RowDefinitions[1].Height = new(70);
        }

        private void ChatInput_LostFocus(object sender, RoutedEventArgs e)
        {
            ContentGrid.RowDefinitions[1].Height = new(50);
        }
        #endregion
    }
}
