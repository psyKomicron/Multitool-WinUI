using Microsoft.UI.Xaml.Media.Imaging;

using Multitool.Net.Irc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    public class Emote
    {
        private const int bufferSize = 1_000;
        private readonly Dictionary<Size, string> urls;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly string mimeType;
#pragma warning restore IDE0052 // Remove unread private members

        private ImageSize currentSize;
        private Regex nameRegex;
        private byte[] hashCode;

        public Emote(Identifier id, string name, Dictionary<Size, string> resourcesLinks, string mimeType = "gif")
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(name)} cannot be empty");
            }

            urls = resourcesLinks;
            this.mimeType = mimeType;
            Id = id;
            Name = name;
        }

        #region properties
        public string CreatorId { get; internal set; }

        public Size Size { get; private set; }

        public Identifier Id { get; }

#if false
        // to support .webm animated images
        public ImageSource Image { get; private set; } 
#else
        public BitmapImage Image { get; private set; }
#endif

        public string Name { get; }

        public Regex NameRegex
        {
            get
            {
                if (nameRegex == null)
                {
                    nameRegex = new($"^{Regex.Escape(Name)}", RegexOptions.Compiled);
                }
                return nameRegex;
            }
        }

        public string Provider { get; internal set; } 

        public string Type { get; internal set; }
        #endregion

        public async Task<byte[]> GetHashCode(HashAlgorithm algo, Encoding encoding)
        {
            if (hashCode == null)
            {
                using MemoryStream stream = new(encoding.GetBytes(urls[Size]));
                hashCode = await algo.ComputeHashAsync(stream);
            }
            return hashCode;
        }

        public override string ToString()
        {
            return Name;
        }

        internal async Task<byte[]> GetImageAsync(HttpClient client, ImageSize size, CancellationToken cancellationToken = default)
        {
            try
            {
                Uri uri = null;
                switch (size)
                {
                    case ImageSize.Small:
                        
                        break;
                    case ImageSize.Medium:
                        
                        break;
                    case ImageSize.Big:
                        
                        break;
                    default:
                        throw new NotImplementedException();
                }

                using HttpResponseMessage reponse = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).AsTask(cancellationToken);
                if (reponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    Trace.TraceError($"Bad request:\n\t{await reponse.Content.ReadAsStringAsync()}\n\t{reponse.Headers}");
                    return null;
                }
                cancellationToken.ThrowIfCancellationRequested();
                reponse.EnsureSuccessStatusCode();

                // Can we use the already buffered data to build the image ?
                IBuffer buffer = await reponse.Content.ReadAsBufferAsync();
                using DataReader dataReader = DataReader.FromBuffer(buffer);

                byte[] readBuffer = new byte[bufferSize];
                List<byte> data = new();

                while (dataReader.UnconsumedBufferLength > 0)
                {
                    if (dataReader.UnconsumedBufferLength <= bufferSize)
                    {
                        readBuffer = new byte[dataReader.UnconsumedBufferLength];
                    }
                    dataReader.ReadBytes(readBuffer);
                    // clean array ?
                    data.AddRange(readBuffer);
                    for (int i = 0; i < readBuffer.Length; i++)
                    {
                        readBuffer[i] = 0;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        data.Clear();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                currentSize = size;
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to download emote \"{Name}\": {ex.Message}");
                throw;
            }
        }

        internal async Task SetImageAsync(byte[] buffer)
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

        internal void SetImage(Uri uri)
        {
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
            Image = new()
            {
                AutoPlay = true,
                UriSource = uri
            };
#endif
        }
    }
}
