using System;
using System.Collections.Concurrent;

namespace Multitool.Net.Twitch.Factories
{
    internal class UserFactory
    {
        private static readonly ConcurrentDictionary<string, User> cache = new();

        public User GetUser(string userName, Span<char> message)
        {
            if (!cache.ContainsKey(userName))
            {
                User user = new(userName)
                {
                    DisplayName = userName,
                };
                cache[userName] = user;
                return user;
            }
            else
            {
                return cache[userName];
            }
        }
    }
}
