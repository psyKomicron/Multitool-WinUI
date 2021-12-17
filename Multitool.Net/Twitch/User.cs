using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Generic;

using Windows.UI;

namespace Multitool.Net.Twitch
{
    public class User
    {
        public User() { }

        public User(string userName)
        {
            Name = userName;
            NameColor = Colors.White;
        }

        public List<Badge> Badges { get; set; }
        public string DisplayName { get; set; }
        public string Id { get; set; }
        public bool IsMod { get; set; }
        public string Name { get; set; }
        public Color NameColor { get; set; }

        public static User CreateSystemUser()
        {
            return new()
            {
                Badges = new(),
                DisplayName = "system",
                Id = string.Empty,
                IsMod = false,
                Name = "system",
                NameColor = Colors.Red,
            };
        }
    }
}
