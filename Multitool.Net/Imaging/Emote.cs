using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Windows.Storage.Streams;

namespace Multitool.Net.Twitch
{
    public class Emote
    {
        private Regex nameRegex;

        public Emote() { }

        public Emote(Id id, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(name)} cannot be empty");
            }

            Id = id;
            Name = name;
        }

        public Id Id { get; internal set; }

        public string Name { get; internal set; }

        public Regex NameRegex
        {
            get
            {
                if (nameRegex == null)
{
                    nameRegex = new($"^{Regex.Escape(Name)}");
                }
                return nameRegex;
            }
            //internal set => nameRegex = value;
        }

        public BitmapImage Image { get; private set; }

        public string Provider { get; internal set; }

        internal async Task SetImage(byte[] buffer)
        {
            Image = new();

            using InMemoryRandomAccessStream stream = new();
            using (DataWriter writer = new(stream))
            {
                writer.WriteBytes(buffer);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
            await Image.SetSourceAsync(stream);
        }
    }
}
