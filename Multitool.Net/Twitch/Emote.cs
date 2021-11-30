using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch
{
    public class Emote
    {
        public Emote() { }

        public Emote(Id id, string name, byte[] image)
        {
            Id = id;
            Name = name;
            NameRegex = new(name);
            Image = image;
        }

        public Id Id { get; internal set; }
        public string Name { get; internal set; }
        public Regex NameRegex { get; internal set; }
        public byte[] Image { get; internal set; }
    }
}
