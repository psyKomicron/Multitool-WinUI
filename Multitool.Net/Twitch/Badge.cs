using System;

namespace Multitool.Net.Twitch
{
    public class Badge
    {
        public Badge() { }

        public Badge(string name, string length)
        {
            Name = name;
            if (int.TryParse(length, out int n))
            {
                Length = n;
            }
            else
            {
                Length = 0;
            }
        }

        public Badge(ReadOnlySpan<char> name, ReadOnlySpan<char> length)
        {
            Name = name.ToString();
            if (int.TryParse(length, out int n))
            {
                Length = n;
            }
            else
            {
                Length = 0;
            }
        }

        public string Name { get; set; }
        public int Length { get; set; }
    }
}
