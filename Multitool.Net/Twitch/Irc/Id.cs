namespace Multitool.Net.Twitch
{
    public struct Id
    {
        private readonly string stringId;

        public Id(string id)
        {
            stringId = id;
        }

        public string StringId => stringId;
    }
}
