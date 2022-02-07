using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Multitool.DAL.Settings;
using Multitool.Net.Imaging;
using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Irc;

using MultitoolWinUI.Helpers;
using MultitoolWinUI.Models;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.System;
using Windows.UI;
using Windows.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Irc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatControl : UserControl, IAsyncDisposable
    {
        private readonly DelayedActionQueue delayedActionQueue = new(1000);
        private readonly SolidColorBrush messageBackground = new(Colors.MediumPurple);
        private readonly SolidColorBrush userSubBackground = new(Colors.MediumPurple)
        {
            Opacity = 0.5
        };
        private readonly SolidColorBrush timestampBrush = new(Colors.White) { Opacity = 0.5 };
        private readonly SolidColorBrush mentionBrush = new(Colors.IndianRed) { Opacity = 0.5 };
        private readonly ConcurrentDictionary<Color, SolidColorBrush> messageColors = new();
        private readonly IIrcClient client;

        private bool joined;
        private bool loaded;
        private bool ctrlOn;

        public ChatControl(IIrcClient client)
        {
            InitializeComponent();
            this.client = client;
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;

            client.MessageReceived += Client_MessageReceived;
            client.Disconnected += Client_Disconnected;
            client.RoomChanged += Client_RoomChanged;
            client.UserTimedOut += Client_UserTimedOut;
            client.UserNotice += Client_UserNotice;
        }

        #region properties
        public string Channel { get; set; }
        public List<Emote> ChannelEmotes { get; } = new();
        public List<Emote> Emotes { get; } = new();
        public TabViewItem Tab { get; set; }
        public FontWeight UserMessagesFontWeight { get; set; } = FontWeights.SemiLight;
        public FontWeight SystemMessagesFontWeight { get; set; } = FontWeights.SemiLight;

        [Setting(typeof(TwitchPage), nameof(TwitchPage.TimestampFormat))]
        public string TimestampFormat { get; set; } = "t";

        [Setting(typeof(TwitchPage), nameof(TwitchPage.ChatMaxNumberOfMessages))]
        public int MaxMessages { get; set; }

        [Setting(typeof(TwitchPage), nameof(TwitchPage.ChatEmoteSize))]
        public double EmoteSize { get; set; } = 30;

        [Setting(typeof(TwitchPage), nameof(TwitchPage.ChatMentionRegex))]
        public Regex MentionRegex { get; set; }
        #endregion

        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
        }

        #region private methods
        private async Task Join()
        {
            if (!joined && client != null && !string.IsNullOrEmpty(Channel))
            {
                try
                {
                    await client.Join(Channel);
                    joined = true;
                    delayedActionQueue.QueueAction(() => DisplayMessage($"Joined {Channel}", string.Empty, messageBackground));
                    ChannelEmotes.AddRange(await EmoteProxy.Get().FetchChannelEmotes(Channel));
                }
                catch (ArgumentException ex)
                {
                    App.TraceError(ex);
                }
                catch (InvalidOperationException ex)
                {
                    App.TraceError(ex);
                }
                catch (Exception ex)
                {
                    App.TraceError(ex);
                }
            }
        }

        private void DisplayMessage(string title, string message, Brush background)
        {
            UpdateInfoBar.Title = title;
            UpdateInfoBar.Message = message;
            UpdateInfoBar.Background = background;
            UpdateInfoBar.IsOpen = true;
        }

        #region ui creation
        private RichTextBlock CreateMessage(Message message)
        {
            RichTextBlock presenter = CreatePresenter();
            Paragraph paragraph = CreateParagraph();

            // timestamp
            paragraph.Inlines.Add(CreateTimestamp());
            // name
            paragraph.Inlines.Add(new Run()
            {
                Text = (string.IsNullOrEmpty(message.Author.DisplayName) ? message.Author.Name : message.Author.DisplayName) + ": ",
                Foreground = messageColors.GetOrAdd(message.Author.NameColor, GetOrCreate),
                FontWeight = FontWeights.SemiBold
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
                        run.FontWeight = words[i][0] == '@' ? FontWeights.Bold : UserMessagesFontWeight;
                    }
                    paragraph.Inlines.Add(run);
                }
                text = true;
            }

            presenter.Blocks.Add(paragraph);
            return presenter;
        }

        private Run CreateTimestamp()
        {
            return new()
            {
                Text = DateTime.Now.ToString(TimestampFormat) + " ",
                Foreground = timestampBrush,
                FontWeight = SystemMessagesFontWeight
            };
        }

        private Paragraph CreateParagraph()
        {
            return new()
            {
                LineHeight = 24,
                CharacterSpacing = 15,
                LineStackingStrategy = LineStackingStrategy.MaxHeight
            };
        }

        private RichTextBlock CreatePresenter()
        {
            return new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true,
            };
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
                    Text = text,
                    FontWeight = FontWeights.SemiLight
                });

                paragraph.Inlines.Add(container);
                return false;
            }
            catch (UriFormatException ex)
            {
                App.TraceError(ex);
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
        #endregion

        #region event handlers

        #region irc events
        private void Client_MessageReceived(IIrcClient sender, Message args)
        {
            if (DispatcherQueue == null) return;

            Chat_ListView.DispatcherQueue.TryEnqueue(() =>
            {
                MessageModel model = new()
                {
                    Content = CreateMessage(args),
                    HorizontalAlignment = args.Author.Name == client.NickName ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };
                if (MentionRegex != null)
                {
                    model.Background = MentionRegex.IsMatch(args.ActualMessage) ? mentionBrush : null;
                }

                Chat_ListView.Items.Add(model);

                NumberOfMessages_TextBlock.Text = Chat_ListView.Items.Count.ToString();
            });

            Chat_ListView.DispatcherQueue.TryEnqueue(() =>
            {
                if (Chat_ListView.Items.Count > MaxMessages)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        if (DispatcherQueue == null)
                        {
                            return;
                        }
                        try
                        {
                            Chat_ListView.Items.RemoveAt(i);
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
            });
            
        }

        private void Client_RoomChanged(IIrcClient sender, RoomStateEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args.States)
                {
                    case RoomStates.EmoteOnlyOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "Emote only on", messageBackground));
                        break;
                    case RoomStates.EmoteOnlyOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "Emote only off", messageBackground));
                        break;

                    case RoomStates.FollowersOnlyOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", $"Followers only on ({args.Data[RoomStates.FollowersOnlyOn]} min)", messageBackground));
                        break;
                    case RoomStates.FollowersOnlyOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "Followers only off", messageBackground));
                        break;

                    case RoomStates.R9KOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "R9K on", messageBackground));
                        break;
                    case RoomStates.R9KOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "R9K off", messageBackground));
                        break;

                    case RoomStates.SlowModeOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", $"Slow mode on ({args.Data[RoomStates.FollowersOnlyOn]} s)", messageBackground));
                        break;
                    case RoomStates.SlowModeOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Room change", "Slow mode off", messageBackground));
                        break;
                }
            });
        }

        private void Client_Disconnected(IIrcClient sender, EventArgs args)
        {
            if (DispatcherQueue != null)
            {
                delayedActionQueue.QueueAction(() => DisplayMessage("Warning", "The client has been disconnected from the channel", messageBackground));
            }
        }

        private void Client_UserTimedOut(IIrcClient sender, UserTimeoutEventArgs args)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                RichTextBlock presenter = CreatePresenter();
                Paragraph paragraph = CreateParagraph();
                
                // timestamp
                paragraph.Inlines.Add(CreateTimestamp());
                if (args.Timeout.Ticks == 0)
                {
                    paragraph.Inlines.Add(new Run()
                    {
                        Text = $"{args.UserName} has been banned",
                        Foreground = new SolidColorBrush(Colors.White)
                        {
                            Opacity = 0.7
                        },
                        FontWeight = SystemMessagesFontWeight
                    });
                }
                else
                {
                    paragraph.Inlines.Add(new Run()
                    {
                        Text = $"{args.UserName} has been timed out for {args.Timeout.TotalSeconds} seconds",
                        Foreground = new SolidColorBrush(Colors.White)
                        {
                            Opacity = 0.7
                        },
                        FontWeight = SystemMessagesFontWeight
                    });
                }

                presenter.Blocks.Add(paragraph);
                Chat_ListView.Items.Add(new MessageModel()
                {
                    Content = presenter
                });
            });
        }

        private void Client_UserNotice(IIrcClient sender, UserNoticeEventArgs args)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                RichTextBlock presenter = CreatePresenter();
                Paragraph systemMessageParagraph = CreateParagraph();

                // timestamp
                systemMessageParagraph.Inlines.Add(CreateTimestamp());

                systemMessageParagraph.Inlines.Add(new Run()
                {
                    Text = args.SystemMessage,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold
                });

                presenter.Blocks.Add(systemMessageParagraph);

                if (!string.IsNullOrEmpty(args.Message))
                {
                    Paragraph userMessageParagraph = CreateParagraph();
                    userMessageParagraph.Inlines.Add(new Run()
                    {
                        Text = args.Message,
                        FontWeight = UserMessagesFontWeight
                    });
                    presenter.Blocks.Add(userMessageParagraph);
                }

                Chat_ListView.Items.Add(new MessageModel()
                {
                    Content = presenter,
                    Background = userSubBackground
                });
            });
        }
        #endregion

        #region ui events
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;
                delayedActionQueue.DispatcherQueue = DispatcherQueue;

                TextBox header = new()
                {
                    PlaceholderText = Channel ?? "Select channel...",
                    BorderThickness = new(0),
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                if (Tab != null)
                {
                    Tab.Header = header;
                    Tab.CloseRequested += Tab_CloseRequested;
                }
                else
                {
                    Grid.SetColumnSpan(header, 2);
                    Grid.SetRow(header, 0);
                    ContentGrid.Children.Add(header);
                }
                header.KeyDown += Header_KeyDown;

                if (!string.IsNullOrEmpty(Channel))
                {
                    await Join();
                }

                try
                {
                    var l = await EmoteProxy.Get().FetchGlobalEmotes();
                    Emotes.AddRange(l);
                }
                catch (Exception ex)
                {
                    App.TraceError(ex);
                }
            }
        }

        private async void Tab_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
        {
            loaded = false;
            try
            {
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            Chat_ListView.Items.Clear();
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            loaded = false;
            if (client != null && client.IsConnected)
            {
                try
                {
                    await client.DisposeAsync();
                }
                catch (Exception)
                {

                }
            }
        }

        private void Header_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && sender is TextBox box)
            {
                Channel = box.Text;
                if (loaded)
                {
                    _ = Join();
                }

                if (Tab != null)
                {
                    Tab.Header = new TextBlock()
                    {
                        Text = Channel,
                        FontWeight = FontWeights.Normal,
                        CharacterSpacing = 45,
                    };
                }
                else
                {
                    ContentGrid.Children.Remove(box);
                }
            }
        }

        private void EmoteGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Emote emote)
            {
                ChatInput.Text += $" {emote} ";
            }
        }

        private void ChatInput_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            ctrlOn = e.Key == VirtualKey.LeftControl;
        }

        private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                try
                {
                    await client.SendMessage(ChatInput.Text);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                if (!ctrlOn)
                {
                    ChatInput.Text = string.Empty;
                }
            }
        }

        private void ChatInput_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            ctrlOn = e.Key != VirtualKey.LeftControl;
        }
        #endregion

        #endregion
    }
}
