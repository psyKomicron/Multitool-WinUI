namespace Multitool.Net.Twitch
{
    public struct Identifier
    {
        private readonly string stringId;
        private readonly int id;

        public Identifier(string id)
        {
            stringId = id;
            this.id = default;
        }

        public Identifier(int id)
        {
            stringId = null;
            this.id = id;
        }

        public string Id => string.IsNullOrEmpty(stringId) ? id.ToString() : stringId;
    }
}
