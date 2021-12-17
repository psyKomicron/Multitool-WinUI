using Multitool.Net.Twitch.IrcTags;
using Multitool.UI;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Multitool.Net.Twitch.Factories
{
    internal class UserFactory
    {
        private readonly ConcurrentDictionary<string, User> cache = new();
        private readonly SemaphoreSlim semaphore = new(1);
        private readonly ColorConverter colorConverter = new(0xFF);

        public User GetUser(Memory<char> message)
        {
            if (TagParser.TryGetTag(message, "user-id", out string userId))
            {
                Dictionary<string, string> tags = TagParser.Parse(message);

                if (cache.ContainsKey(userId))
                {
                    User user = cache[userId];
                    AssertSame(user, tags);
                    return user;
                }
                else
                {
                    if (semaphore.Wait(10))
                    {
                        cache.AddOrUpdate(userId, BuildUser(tags), UpdateCallback);
                        semaphore.Release();
                    }
                }
            }
            return null;
        }

        private void AssertSame(User user, Dictionary<string, string> tags)
        {

        }

        private User BuildUser(Dictionary<string, string> tags)
        {
            User user = new()
            {
                DisplayName = tags["display-name"] == string.Empty ? "anonymous" : tags["display-name"],
                Id = tags["user-id"],
                IsMod = tags["mod"] == "1",
                NameColor = colorConverter.ConvertFromString(tags["color"]),
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

        private User UpdateCallback(string key, User user)
        {
            return user;
        }
    }
}
