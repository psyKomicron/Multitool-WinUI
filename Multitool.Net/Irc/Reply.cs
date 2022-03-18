namespace Multitool.Net.Irc
{
    public class Reply
    {
        public Identifier ParentMessageId { get; set; }
        public Identifier ParentUserId { get; set; }
        public string ParentUserLogin { get; set; }
        public string ParentUserDisplayName { get; set; }
        public string ParentMessageBody { get; set; }
    }
}
