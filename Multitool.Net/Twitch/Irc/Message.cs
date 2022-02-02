using System;

namespace Multitool.Net.Twitch
{
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

        public Identifier Id { get; set; }

#nullable enable
        public Reply? Reply { get; set; }
#nullable disable

        public Identifier ChannelId { get; set; }

        public DateTime ServerTimestamp { get; set; }

        public string ActualMessage => message;

        public override string ToString()
        {
            return message;
        }
    }
}
