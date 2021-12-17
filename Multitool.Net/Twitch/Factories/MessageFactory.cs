using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Net.Twitch.Factories
{
    internal class MessageFactory
    {
        private readonly UserFactory factory = new();

        public Message GetMessage(Memory<char> memory)
        {
            User author = factory.GetUser(memory);
            Message message = new()
            {
                Author = author
            };

            return message;
        }
    }
}
