namespace Multitool.Net.Twitch
{
    public struct Id
    {
        private readonly string stringId;
        private readonly int id;

        public Id(string id)
        {
            stringId = id;
            this.id = default;
        }

        public Id(int id)
        {
            stringId = null;
            this.id = id;
        }

        public string StringId => stringId;
    }
}
