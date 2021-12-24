using Multitool.UI;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Windows.UI;

namespace Multitool.Net.Twitch.Factories
{
    internal class MessageFactory
    {
        private const string userKey = "user-name";
        private readonly ConcurrentDictionary<string, User> cache = new();
        private readonly SemaphoreSlim semaphore = new(1);
        private readonly ColorConverter colorConverter = new(0xFF);

        public bool UseLocalTimestamp { get; set; }

        public Message CreateMessage(Memory<char> memory)
        {
            Dictionary<string, string> tags = new();
            int index = 0;
            int lastSlice = 0;
            while (index < memory.Length)
            {
                if (memory.Span[index] == ';')
                {
                    Memory<char> part = memory[lastSlice..index];
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
                else if (memory.Span[index] == ' ')
                {
                    index++;
                    break;
                }
                index++;
            }

            ReadOnlySpan<char> data = memory.Span[(index + 1)..];
            index = 0; // reset index since we are going to work with a slice of the original payload
            while (index < data.Length && data[index] != '!')
            {
                index++;
            }
            ReadOnlySpan<char> userName = data[..index];
            tags.Add("user-name", userName.ToString());

            lastSlice = index;
            while (index < data.Length && data[index] != ':')
            {
                index++;
            }
            index++;
            ReadOnlySpan<char> text = data[index..];

            User author = CreateUser(tags);
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

        public User CreateUser(Dictionary<string, string> tags)
        {
            string userId = tags[userKey];
            if (cache.ContainsKey(userId))
            {
                User user = cache[userId];
                //AssertSame(user, tags);
                return user;
            }
            else
            {
                User user = BuildUser(tags);
                if (semaphore.Wait(100))
                {
                    cache.AddOrUpdate(userId, user, UpdateCallback);
                    semaphore.Release();
                }
                else
                {
                    Debug.WriteLine($"User.CreateUser > Failed to get semaphore, user ({user.Name}) was created but not cached");
                }
                return user;
            }
        }

        private User BuildUser(Dictionary<string, string> tags)
        {
            Color nameColor;
            try
            {
                string stringColor = tags["color"];
                if (!string.IsNullOrEmpty(stringColor))
                {
                    nameColor = colorConverter.ConvertFromHexaString(stringColor);
                }
                else
                {
                    nameColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                }
            }
            catch
            {
                nameColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            }

            User user = new()
            {
                DisplayName = tags["display-name"],
                Name = tags["user-name"],
                Id = tags["user-id"],
                IsMod = tags["mod"] == "1",
                NameColor = nameColor,
                Badges = new()
            };
            // badges
            string badgeTag = tags["badges"];
            if (badgeTag != string.Empty)
            {
                ReadOnlySpan<char> badges = new(badgeTag.ToCharArray());
                int length = 0;
                for (int i = 0; i < badges.Length; i++)
                {
                    if (badges[i] == '/')
                    {
                        ReadOnlySpan<char> badgeName = badges.Slice(i - length, length);
                        length = 0;

                        for (i += 1; i < badges.Length; i++)
                        {
                            if (badges[i] == ',')
                            {
                                ReadOnlySpan<char> badgeValue = badges.Slice(i - length, length);
                                user.Badges.Add(new(badgeName, badgeValue));
                            }
                            else
                            {
                                length++;
                            }
                        }
                        length = 0;
                    }
                    else
                    {
                        length++;
                    }
                }
            }
            return user;
        }

        private void AssertSame(User user, Dictionary<string, string> tags)
        {

        }

        private static User UpdateCallback(string key, User user)
        {
            return user;
        }
    }
}
