using Multitool.Drawing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Windows.UI;

namespace Multitool.Net.Irc.Factories
{
    internal class UserFactory
    {
        private const string userKey = "user-name";
        private readonly ConcurrentDictionary<string, User> cache = new();
        private readonly SemaphoreSlim semaphore = new(1);
        private readonly ColorConverter colorConverter = new();

        public User GetOrCreateUser(Dictionary<string, string> tags)
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

#nullable enable
        public User? GetUser(string userId)
        {
            if (cache.ContainsKey(userId))
            {
                return cache[userId];
            }
            return null;
        }
#nullable disable

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
                Debug.WriteLine("Failed to convert color: " + tags["color"]);
                nameColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            }

            User user = new()
            {
                DisplayName = tags["display-name"],
                Name = tags["user-name"],
                Id = tags["user-id"],
                IsMod = tags["mod"] == "1",
                NameColor = nameColor
            };
            // badges
            string badgeTag = tags["badges"];
            if (badgeTag != string.Empty)
            {
                user.Badges = new();
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

        /*private void AssertSame(User user, Dictionary<string, string> tags)
        {

        }*/

        private static User UpdateCallback(string key, User user)
        {
            return user;
        }
    }
}
