using System;

namespace Multitool.Net.Irc
{
#nullable enable
    public class Message
    {
        private readonly string message;

        public Message()
        {
            message = string.Empty;
        }

        public Message(string message)
        {
            this.message = message;
        }

        public User Author { get; set; }
        public Id Id { get; set; }
        public Reply? Reply { get; set; }
        public Id ChannelId { get; set; }
        public DateTime ServerTimestamp { get; set; }

        public override string ToString()
        {
            return message;
        }
    }
}
