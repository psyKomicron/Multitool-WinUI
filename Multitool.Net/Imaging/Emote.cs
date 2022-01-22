using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Multitool.Net.Twitch;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Multitool.Net.Imaging
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

        public string ChannelOwner { get; internal set; }

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

#if false
        public ImageSource Image { get; private set; } 
#else
        public BitmapImage Image { get; private set; }
#endif

        public string Provider { get; internal set; }

        public override string ToString()
        {
            return Name;
        }

        internal async Task SetImage(byte[] buffer, string mimeType)
        {
            using InMemoryRandomAccessStream stream = new();
            using (DataWriter writer = new(stream))
            {
                writer.WriteBytes(buffer);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
#if false
            if (mimeType == "image/webp")
            {
                var encoder = await BitmapDecoder.CreateAsync(BitmapDecoder.WebpDecoderId, stream).AsTask();
                var softBitmap = await encoder.GetSoftwareBitmapAsync();
                if (softBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || softBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softBitmap = SoftwareBitmap.Convert(softBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(softBitmap);
                Image = source;
            }
            else
            {
                BitmapImage image = new();
                await image.SetSourceAsync(stream);
                Image = image;
            }
#else
            Image = new();
            await Image.SetSourceAsync(stream);
#endif
        }
    }
}
