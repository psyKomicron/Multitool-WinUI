﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Multitool.Net.Twitch;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MultitoolWinUI.Models
{
    public sealed class ChatMessageModel
    {
        private const string dateTimeFormat = "t";
        private static readonly Regex linkRegex = new(@"(?:(?:https?|ftp|file):\/\/|www\.|ftp\.)(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[-A-Z0-9+&@#\/%=~_|$?!:,.])*(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[A-Z0-9+&@#\/%=~_|$])", RegexOptions.Multiline | RegexOptions.Compiled);

        public ChatMessageModel() { }

        public ChatMessageModel(Message message)
        {
            Timestamp = message.ServerTimestamp.ToString(dateTimeFormat);
            UserName = message?.Author?.DisplayName;

            Message = new TextBlock()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true
            };

            if (message.Author != null)
            {
                UserName = message.Author.DisplayName;
                NameColor = new(message.Author.NameColor);
            }
        }

        public string Timestamp { get; set; }
        public FrameworkElement Message { get; set; }
        public string UserName { get; set; }
        public SolidColorBrush NameColor { get; set; }
        public Image UserBadge { get; set; }
    }
}
