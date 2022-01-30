
using System;
using System.Collections.Generic;

namespace Multitool.Net.Twitch.Factories
{
    internal class MessageFactory
    {
        private readonly UserFactory userFactory = new();

        public bool UseLocalTimestamp { get; set; }

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

            User author = userFactory.CreateUser(tags);
            Message message = new(text.ToString())
            {
                Author = author
            };

            if (UseLocalTimestamp)
            {
                message.ServerTimestamp = DateTime.Now;
            }
            else
            {
                try
                {
                    long epoch = long.Parse(tags["tmi-sent-ts"]);
                    DateTimeOffset result = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    message.ServerTimestamp = result.LocalDateTime;
                }
                catch (FormatException)
                {
                    message.ServerTimestamp = DateTime.Now;
                }
                catch (ArgumentException)
                {
                    message.ServerTimestamp = DateTime.Now;
                }
            }

            return message;
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
    }
}
