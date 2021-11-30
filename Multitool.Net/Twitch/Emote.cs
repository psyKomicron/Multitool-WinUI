using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch
{
    public class Emote
    {
        public Emote() { }

        public Emote(int id, string name, byte[] image)
        {
            Id = id;
            Name = name;
            NameRegex = new(name);
            Image = image;
        }

        public int Id { get; init; }
        public string Name { get; init; }
        public Regex NameRegex { get; init; }
        public byte[] Image { get; init; }
    }
}
