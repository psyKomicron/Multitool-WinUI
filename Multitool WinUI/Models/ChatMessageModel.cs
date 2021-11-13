using System;

namespace MultitoolWinUI.Models
{
    public sealed class ChatMessageModel
    {
        public ChatMessageModel()
        {
            Timestamp = string.Empty;
            Message = string.Empty;
            UserName = string.Empty;
        }

        public ChatMessageModel(string userName, string message)
        {
            Timestamp = DateTime.Now.ToString("t");
            UserName = userName;
            Message = message;
        }

        public string Timestamp { get; }
        public string Message { get; }
        public string UserName { get; }
    }
}
