using System;
using System.Collections.Generic;

namespace Multitool.Net.Twitch.IrcTags
{
    internal static class TagParser
    {
        public static Dictionary<string, string> Parse(Memory<char> message)
        {
            Dictionary<string, string> tags = new();
            int index = 0;
            int lastSlice = 0;
            while (index < message.Length)
            {
                if (message.Span[index] == ';')
                {
                    Memory<char> part = message[lastSlice..index];
                    for (int i = 0; i < part.Length; i++)
                    {
                        if (part.Span[i] == '=')
                        {
                            Memory<char> tag, value;
                            tag = part[..i];
                            value = part[(i + 1)..];

                            tags.Add(tag.ToString(), value.ToString());
                        }
                    }
                    lastSlice = index + 1;
                }
                index++;
            }

            return tags;
        }

        public static bool TryGetTag(Memory<char> message, string tagName, out string tagValue)
        {
            int index = 0;
            int lastSlice = 0;
            while (index < message.Length)
            {
                if (message.Span[index] == ';')
                {
                    Memory<char> part = message[lastSlice..index];
                    for (int i = 0; i < part.Length; i++)
                    {
                        if (part.Span[i] == '=')
                        {
                            Memory<char> tag, value;
                            tag = part[..i];

                            bool equals = true;
                            for (int n = 0; n < tag.Length; n++)
                            {
                                if (tag.Span[n] != tagName[n])
                                {
                                    equals = false;
                                    break;
                                }
                            }
                            if (equals)
                            {
                                value = part[(i + 1)..];
                                tagValue = value.ToString();

                                return true;
                            }
                        }
                    }
                    lastSlice = index + 1;
                }
                index++;
            }
            tagValue = null;
            return false;
        }
    }
}
