using System;

namespace Multitool.Net.Twitch
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

        public Identifier Id { get; set; }

        public Reply? Reply { get; set; }

        public Identifier ChannelId { get; set; }

        public DateTime ServerTimestamp { get; set; }

#if DEBUG
        public string ActualMessage => message;
#endif

        public override string ToString()
        {
            return message;
        }
    }
}
