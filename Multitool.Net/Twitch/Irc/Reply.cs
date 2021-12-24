namespace Multitool.Net.Twitch
{
    public class Reply
    {
        public Id ParentMessageId { get; set; }
        public Id ParentUserId { get; set; }
        public string ParentUserLogin { get; set; }
        public string ParentUserDisplayName { get; set; }
        public string ParentMessageBody { get; set; }
    }
}
