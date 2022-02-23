using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;

using Windows.System;

namespace Multitool.Net.Embeds
{
#if DEBUG
    internal static class EmbedFetcherHelper
    {
        public static async Task DumpClean(string content, bool open)
        {
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("clean dump.dat", CreationCollisionOption.GenerateUniqueName);
            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using DataWriter writer = new(stream);

            StringBuilder builder = new();
            for (int n = 0; n < content.Length; n++)
            {
                if (content[n] != '\n')
                {
                    builder.Append(content[n]);
                }
                else
                {
                    builder.Append(' ').Append(' ');
                }
            }

            writer.WriteString(builder.ToString());
            await writer.StoreAsync();
            await stream.FlushAsync();

            Trace.TraceInformation($"Dumped raw to {file.Path}");
            if (open && !await Launcher.LaunchFileAsync(file))
            {
                Trace.TraceWarning($"Failed to open dump file ({file.Name})");
            }
        }

        public static async Task DumpRaw(string content, bool open)
        {
            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("raw dump.dat", CreationCollisionOption.GenerateUniqueName);

            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using DataWriter writer = new(stream);
            writer.WriteString(content);
            await writer.StoreAsync();
            await stream.FlushAsync();

            Trace.TraceInformation($"Dumped html parsing to file {file.Path}");
            if (open && !await Launcher.LaunchFileAsync(file))
            {
                Trace.TraceWarning($"Failed to open dump file ({file.Name})");
            }
        }

        public static async Task DumpTags(List<Tag> tags)
        {
            StringBuilder builder = new();
            foreach (var tag in tags)
            {
                builder.AppendLine("meta: ");
                foreach (var attr in tag.Attributes)
                {
                    builder.Append('\t').Append(attr.Key).Append(':').Append(' ').AppendLine(attr.Value);
                }
            }

            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("tag dump.dat", CreationCollisionOption.GenerateUniqueName);

            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using DataWriter writer = new(stream);
            writer.WriteString(builder.ToString());
            await writer.StoreAsync();
            await stream.FlushAsync();

            Trace.TraceInformation($"Dumped html parsing to file {file.Path}");
            if (!await Launcher.LaunchFileAsync(file))
            {
                Trace.TraceWarning($"Failed to open dump file ({file.Name})");
            }
        }
    }
#endif
}
