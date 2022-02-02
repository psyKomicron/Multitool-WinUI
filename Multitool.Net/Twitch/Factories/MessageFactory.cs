
using Multitool.Net.Twitch.Irc;

using System;
using System.Collections.Generic;
using System.Text;

namespace Multitool.Net.Twitch.Factories
{
    internal class MessageFactory
    {
        private readonly UserFactory userFactory = new();

        public bool UseLocalTimestamp { get; set; }
        internal UserFactory UserFactory => userFactory;

        public Message CreateMessage(ReadOnlyMemory<char> memory)
        {
            int index = 0;
            Dictionary<string, string> tags = ParseTags(memory, ref index);

            ReadOnlySpan<char> data = memory.Span[(index + 1)..];
            index = 0; // reset index since we are going to work with a slice of the original payload
            while (index < data.Length && data[index] != '!')
            {
                index++;
            }
            ReadOnlySpan<char> userName = data[..index];
            tags.Add("user-name", userName.ToString());

            while (index < data.Length && data[index] != ':')
            {
                index++;
            }
            index++;

            ReadOnlySpan<char> text = data[index..];

            User author = userFactory.GetOrCreateUser(tags);
            Message message = new(text.ToString())
            {
                Author = author,
                ServerTimestamp = GetTimeStamp(tags)
            };

            return message;
        }

        public static UserNoticeEventArgs CreateUserNotice(ReadOnlyMemory<char> memory, Dictionary<string, string> tags, int index)
        {
            UserNoticeEventArgs notice = new();

            ReadOnlyMemory<char> remains = memory[index..];
            index = 1;
            for (; index < remains.Length; index++)
            {
                if (remains.Span[index] == ':')
                {
                    index++;
                    break;
                }
            }
            if (tags.TryGetValue("system-msg", out string systemMessage))
            {
                int i = 0;
                StringBuilder cleanedMessage = new();
                while (i < systemMessage.Length)
                {
                    if (systemMessage[i] == '\\')
                    {
                        if (i + 1 < systemMessage.Length && systemMessage[i + 1] == 's')
                        {
                            cleanedMessage.Append(' ');
                            i += 2;
                        }
                        else
                        {
                            cleanedMessage.Append('\\');
                            i++;
                        }
                    }
                    else
                    {
                        cleanedMessage.Append(systemMessage[i]);
                        i++;
                    }
                }
                notice.SystemMessage = cleanedMessage.ToString();
            }
            notice.Message = remains[index..].ToString();
            string noticeType = tags["msg-id"];
            switch (noticeType) 
            {
                case "sub":
                    notice.NoticeType = NoticeType.Sub;
                    break;
                case "resub":
                    notice.NoticeType = NoticeType.ReSub;
                    break;
                case "subgift":
                    notice.NoticeType = NoticeType.SubGift;
                    break;
                case "anonsubgift":
                    notice.NoticeType = NoticeType.AnonSubGift;
                    break;
                case "submysterygift":
                    notice.NoticeType = NoticeType.SubMisteryGift;
                    break;
                case "giftpaidupgrade":
                    notice.NoticeType = NoticeType.GiftPaidUpgrade;
                    break;
                case "rewardgift":
                    notice.NoticeType = NoticeType.RewardGift;
                    break;
                case "anongiftpaidupgrade":
                    notice.NoticeType = NoticeType.AnonGiftPaidUpgrade;
                    break;
                case "raid":
                    notice.NoticeType = NoticeType.Raid;
                    break;
                case "unraid":
                    notice.NoticeType = NoticeType.UnRaid;
                    break;
                case "ritual":
                    notice.NoticeType = NoticeType.Ritual;
                    break;
                case "bitsbadgetier":
                    notice.NoticeType = NoticeType.BitsBadgeTier;
                    break;
            }

            return notice;
        }

        public static Dictionary<string, string> ParseTags(ReadOnlyMemory<char> memory, ref int index)
        {
            Dictionary<string, string> tags = new();
            index = 1;
            int lastSlice = 1;
            while (index < memory.Length)
            {
                if (memory.Span[index] == ';')
                {
                    ReadOnlyMemory<char> part = memory[lastSlice..index];
                    for (int i = 0; i < part.Length; i++)
                    {
                        if (part.Span[i] == '=')
                        {
                            ReadOnlyMemory<char> tag, value;
                            tag = part[..i];
                            value = part[(i + 1)..];

                            tags.Add(tag.ToString(), value.ToString());
                        }
                    }
                    lastSlice = index + 1;
                }
                else if (memory.Span[index] == ' ')
                {
                    index++;
                    break;
                }
                index++;
            }
            return tags;
        }

        private DateTime GetTimeStamp(Dictionary<string, string> tags)
        {
            if (UseLocalTimestamp)
            {
                return DateTime.Now;
            }
            else
            {
                try
                {
                    long epoch = long.Parse(tags["tmi-sent-ts"]);
                    DateTimeOffset result = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    return result.LocalDateTime;
                }
                catch (FormatException)
                {
                    return DateTime.Now;
                }
                catch (ArgumentException)
                {
                    return DateTime.Now;
                }
            }
        }
    }
}
