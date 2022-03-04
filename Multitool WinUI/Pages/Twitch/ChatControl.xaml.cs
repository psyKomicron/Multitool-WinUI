using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Multitool.Collections;
using Multitool.Data.Settings;
using Multitool.Data.Settings.Converters;
using Multitool.Net.Embeds;
using Multitool.Net.Imaging;
using Multitool.Net.Irc;
using Multitool.Net.Irc.Twitch;

using MultitoolWinUI.Controls;
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
    public sealed partial class ChatControl : UserControl, IAsyncDisposable, IIrcSubscriber
    {
        private readonly DelayedActionQueue delayedActionQueue = new(1000);
        private readonly ConcurrentDictionary<Color, SolidColorBrush> messageColors = new();
        private readonly IIrcClient client;
        //private readonly IEmbedFetcher embedFetcher = new YoutubeEmbedFetcher();
        private readonly SolidColorBrush userSubBackground = new(Colors.MediumPurple)
        {
            Opacity = 0.5
        };
        private readonly SolidColorBrush timestampBrush = new(Colors.White)
        {
            Opacity = 0.5
        };
        private readonly SolidColorBrush mentionBrush = new(Colors.IndianRed)
        {
            Opacity = 0.3
        };

        private bool joined;
        private bool loaded;
        private bool ctrlOn;

        public ChatControl(IIrcClient client)
        {
            this.client = client;
            client.Subscribe(this);

            InitializeComponent();
            Loaded += OnLoaded;
            App.MainWindow.Closed += MainWindow_Closed;

            delayedActionQueue.DispatcherQueue = DispatcherQueue;
            delayedActionQueue.QueueEmpty += DelayedActionQueue_QueueEmpty;

            try
            {
                App.UserSettings.Load(this);
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Chat settings failed to load.");
            }
        }

        #region properties
        public string Channel { get; set; }

        public List<Emote> ChannelEmotes { get; } = new();
        
        public List<Emote> Emotes { get; } = new();

        public IEmoteFetcher EmoteFetcher { get; set; }

        public TabViewItem Tab { get; set; }

        public FontWeight UserMessagesFontWeight { get; set; } = FontWeights.Normal;

        public FontWeight SystemMessagesFontWeight { get; set; } = FontWeights.SemiLight;

        #region settings
        [Setting(30)]
        public double EmoteSize { get; set; }

        [Setting(1000)]
        public int MaxMessages { get; set; }

        [Setting(typeof(RegexSettingConverter))]
        public Regex Mention { get; set; }

        [Setting("t")]
        public string TimestampFormat { get; set; }

        [Setting]
        public bool ReplyWithAt { get; set; }
        #endregion

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

                    delayedActionQueue.QueueAction(() => DisplayMessage($"Joined {Channel}"));
                    //ChannelEmotes.AddRange(await EmoteProxy.Get().FetchChannelEmotes(Channel));
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

        private void DisplayMessage(string message)
        {
            UpdateInfoBar.Message = message;
            UpdateInfoBar.IsOpen = true;
        }

        #region ui creation
        private FrameworkElement CreateMessage(Message message)
        {
            RichTextBlock presenter = CreatePresenter();
            StackPanel embedPanel = null;
            Paragraph paragraph = CreateParagraph();

            // timestamp
            paragraph.Inlines.Add(CreateTimestamp());
            // name
            paragraph.Inlines.Add(new Run()
            {
                Text = $"{(string.IsNullOrEmpty(message.Author.DisplayName) ? message.Author.Name : message.Author.DisplayName)}: ",
                Foreground = messageColors.GetOrAdd(message.Author.NameColor, GetOrCreate),
                FontWeight = FontWeights.SemiBold
            });

            string[] words = message.ToString().Split(' ');
            List<string> links = new();
            bool text = true;
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    if (Tool.IsRelativeUrl(words[i]))
                    {
                        /*if (embedFetcher.CanFetch(words[i]) && !links.Contains(words[i]))
                        {
                            links.Add(words[i]);
                            int copy = i;
                            var embed = embedFetcher.Fetch(words[copy]);
                            if (embedPanel == null)
                            {
                                embedPanel = new()
                                {
                                    Orientation = Orientation.Vertical,
                                    Spacing = 5
                                };
                            }
                            embed.ContinueWith((Task<Embed> task) =>
                            {
                                embedPanel.DispatcherQueue.TryEnqueue(() =>
                                {
                                    embedPanel.Children.Add(new EmbedView(task.Result)
                                    {
                                        HorizontalAlignment = HorizontalAlignment.Stretch
                                    });
                                });
                            });
                            text = false;
                        }*/
                        text = PutHyperlink(paragraph, words[i]);
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
                            run.FontWeight = words[i][0] == '@' ? FontWeights.SemiBold : UserMessagesFontWeight;
                        }
                        paragraph.Inlines.Add(run);
                    }
                    text = true; 
                }
            }
            presenter.Blocks.Add(paragraph);
            if (embedPanel != null)
            {
                Grid grid = new();
                RowDefinition row1 = new();
                RowDefinition row2 = new()
                {
                    Height = GridLength.Auto
                };
                grid.RowDefinitions.Add(row1);
                grid.RowDefinitions.Add(row2);
                Grid.SetRow(presenter, 0);
                Grid.SetRow(embedPanel, 1);
                grid.Children.Add(presenter);
                grid.Children.Add(embedPanel);
                return grid;
            }
            else
            {
                return presenter;
            }
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
                LineHeight = EmoteSize,
                CharacterSpacing = 15,
                LineStackingStrategy = LineStackingStrategy.MaxHeight
            };
        }

        private static RichTextBlock CreatePresenter()
        {
            return new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true
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
                    Text = $"{text} ",
                    FontWeight = FontWeights.SemiLight
                });
                paragraph.Inlines.Add(container);
                return false;
            }
            catch (UriFormatException ex)
            {
                Trace.TraceError($"Cannot create uri: {text}");
                App.TraceError(ex);
                return true;
            }
        }

        private void PutImage(Paragraph paragraph, Emote emote)
        {
            Image image = new()
            {
                Source = emote.Image,
                Height = EmoteSize,
                //Margin = new(0, 0, 0, -((EmoteSize / 2) - 4)),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            InlineUIContainer imageContainer = new()
            {
                Child = image,
                KeyTipPlacementMode = KeyTipPlacementMode.Center
            };
            ToolTipService.SetToolTip(image, new TextBlock()
            {
                Text = $"{emote.Name}, {emote.Provider}"
            });
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
        public void OnMessageReceived(IIrcClient sender, Message args)
        {
            if (DispatcherQueue == null) return;
            
            Chat_ListView.DispatcherQueue.TryEnqueue(() =>
            {
                var content = CreateMessage(args);

                MessageModel model = new(args)
                {
                    Content = content
                };
                content.IsDoubleTapEnabled = true;
                content.DoubleTapped += model.OnReply;
                model.Reply += OnMessageReply;

                if (Mention != null)
                {
                    model.Background = Mention.IsMatch(args.ActualMessage) ? mentionBrush : null;
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

        public void OnRoomChanged(IIrcClient sender, RoomStateEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args.States)
                {
                    case RoomStates.EmoteOnlyOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Emote only on"));
                        break;
                    case RoomStates.EmoteOnlyOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Emote only off"));
                        break;

                    case RoomStates.FollowersOnlyOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage($"Followers only on ({args.Data[RoomStates.FollowersOnlyOn]} min)"));
                        break;
                    case RoomStates.FollowersOnlyOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Followers only off"));
                        break;

                    case RoomStates.R9KOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage("R9K on"));
                        break;
                    case RoomStates.R9KOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("R9K off"));
                        break;

                    case RoomStates.SlowModeOn:
                        delayedActionQueue.QueueAction(() => DisplayMessage($"Slow mode on ({args.Data[RoomStates.FollowersOnlyOn]} s)"));
                        break;
                    case RoomStates.SlowModeOff:
                        delayedActionQueue.QueueAction(() => DisplayMessage("Slow mode off"));
                        break;
                }
            });
        }

        public void OnDisconnected(IIrcClient sender, EventArgs args)
        {
            if (DispatcherQueue != null)
            {
                delayedActionQueue.QueueAction(() => DisplayMessage("The client has been disconnected from the channel"));
            }
        }

        public void OnUserTimedOut(IIrcClient sender, UserTimeoutEventArgs args)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                RichTextBlock presenter = CreatePresenter();
                Paragraph paragraph = CreateParagraph();
                
                // timestamp
                paragraph.Inlines.Add(CreateTimestamp());
                paragraph.Inlines.Add(new Run()
                {
                    Text = args.Timeout.Ticks == 0 ? $"{args.UserName} has been banned" : $"{args.UserName} has been timed out for {args.Timeout.TotalSeconds} seconds",
                    Foreground = new SolidColorBrush(Colors.White)
                    {
                        Opacity = 0.7
                    },
                    FontWeight = SystemMessagesFontWeight
                });

                presenter.Blocks.Add(paragraph);
                Chat_ListView.Items.Add(new MessageModel(null)
                {
                    Content = presenter
                });
            });
        }

        public void OnUserNotice(IIrcClient sender, UserNoticeEventArgs args)
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

                Chat_ListView.Items.Add(new MessageModel(null)
                {
                    Content = presenter,
                    Background = userSubBackground
                });
            });
        }
        #endregion

        #region ui events
        private void OnMessageReply(MessageModel sender, Message args)
        {
            ChatInput.Text += ReplyWithAt ? $"@{args.Author} " : $"{args.Author} ";
            ChatInput.Focus(FocusState.Programmatic);
            ChatInput.SelectionStart = ChatInput.Text.Length;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;

                #region ui/textbox update
                TextBox header = new()
                {
                    PlaceholderText = Channel ?? "Select channel",
                    BorderThickness = new(0),
                    FontWeight = FontWeights.SemiLight
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
                #endregion

                if (!string.IsNullOrEmpty(Channel))
                {
                    try
                    {
                        await Join();
                    }
                    catch (Exception ex)
                    {
                        App.TraceError(ex, $"Failed to join {Channel}");
                        return;
                    }
                }

                try
                {
                    List<Emote> emotes = await EmoteFetcher.FetchGlobalEmotes();
                    Emotes.AddRange(emotes);
                }
                catch (Exception ex)
                {
                    App.TraceError(ex, $"Failed to load emotes for {Channel}");
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
            delayedActionQueue.Clear();
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
                        FontWeight = FontWeights.Normal
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

        private void ChatInput_PreviewKeyDown(object sender, KeyRoutedEventArgs e) => ctrlOn = e.Key == VirtualKey.LeftControl;

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

        private void ChatInput_KeyUp(object sender, KeyRoutedEventArgs e) => ctrlOn = e.Key != VirtualKey.LeftControl;

        private void DelayedActionQueue_QueueEmpty(DelayedActionQueue sender, System.Timers.ElapsedEventArgs args) => UpdateInfoBar.IsOpen = false;
        #endregion

        #endregion
    }
}
