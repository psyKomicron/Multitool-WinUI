using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.Web.Http;

namespace Multitool.Net.Embeds
{
    public class YoutubeEmbedFetcher : IEmbedFetcher, IDisposable
    {
        private static readonly Regex youtubeRegex = new(@"^(https|http)://www\.youtube\.com/watch\?.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly HttpClient client = new();

        public bool CanFetch(string url)
        {
            return youtubeRegex.IsMatch(url);
        }

        public void Dispose()
        {
            client.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<Embed> Fetch(string url)
        {
            if (youtubeRegex.IsMatch(url))
            {
                using HttpResponseMessage response = await client.GetAsync(new(url));
                response.EnsureSuccessStatusCode();

                string raw = await response.Content.ReadAsStringAsync();
#if DEBUG
                _ = EmbedFetcherHelper.DumpRaw(raw, false);
#endif
                ReadOnlyMemory<char> memory = new(raw.ToCharArray());
                List<Tag> tags = new();
#if DEBUG
                nint iterations = 0;
#endif
                int i = 0;
                while (i < memory.Length)
                {
                    if (memory.Span[i] == '<') // opening tag
                    {
                        if ((i + 1) < memory.Length)
                        {
                            if (IsMeta(memory, i + 1))
                            {
                                tags.Add(ParseTag(memory, ref i));
                            }
                            else if (IsSpan(memory, i + 1))
                            {
                                ParseSpan(memory, ref i, tags);
                            }    
                            else
                            {
                                // we need to skip the tag
                                i++;
                            } 
                        }
                    }
                    else
                    {
                        i++;
                    }

#if DEBUG
                    iterations++; 
#endif
                }
#if DEBUG
                Debug.WriteLine($"Iterations: {iterations} for {memory.Length} chars.");
#endif

                var embed = CreateEmbed(tags);
                embed.Url = new(url);
                return embed;
            }
            else
            {
                throw new FormatException($"Uri does not match the expected format (expected: {youtubeRegex})");
            }
        }

        private static YoutubeEmbed CreateEmbed(List<Tag> tags)
        {
            YoutubeEmbed embed = new();
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag.Attributes.TryGetValue("itemprop", out string name))
                {
                    if (name == "name")
                    {
                        if (tag.Attributes.TryGetValue("itemtype", out string itemtype))
                        {
                            if (itemtype == "author")
                            {
                                embed.Author = tag.Attributes["content"];
                            }
                        }
                        else
                        {
                            embed.Title = tag.Attributes["content"];
                        }
                    }
                    else if (name == "description")
                    {
                        embed.Description = tag.Attributes["content"];
                    }
                    else if (name == "paid")
                    {
                        embed.Paid = bool.Parse(tag.Attributes["content"]);
                    }
                    else if (name == "channelId")
                    {
                        embed.ChannelId = tag.Attributes["content"];
                    }
                    else if (name == "videoId")
                    {
                        embed.VideoId = tag.Attributes["content"];
                    }
                    else if (name == "duration")
                    {
                        string duration = tag.Attributes["content"];
                        Span<char> span = new(duration.ToArray());
                        int n = 2;
                        while (span[n] != 'M')
                        {
                            n++;
                        }
                        Span<char> minutes = span[2..n];
                        int k = n + 1;
                        while (span[n] != 'S')
                        {
                            n++;
                        }
                        Span<char> seconds = span[k..n];

                        int m = int.Parse(minutes);
                        int s = int.Parse(seconds);
                        embed.Duration = new(0, m, s);
                    }
                    else if (name == "unlisted")
                    {
                        embed.Unlisted = bool.Parse(tag.Attributes["content"]);
                    }
                    else if (name == "url")
                    {
                        if (tag.Attributes.TryGetValue("itemtype", out string itemtype))
                        {
                            if (itemtype == "thumbnail")
                            {
                                embed.ThumbnailUrl = new(tag.Attributes["href"]);
                            }
                        }
                    }
                    else if (name == "embedUrl")
                    {
                        embed.EmbedUrl = new(tag.Attributes["content"]);
                    }
                    else if (name == "isFamilyFriendly")
                    {
                        embed.FamilyFriendly = bool.Parse(tag.Attributes["content"]);
                    }
                    else if (name == "regionsAllowed")
                    {
                        // too lazy right now
                    }
                    else if (name == "interactionCount")
                    {
                        embed.Interactions = long.Parse(tag.Attributes["content"]);
                    }
                    else if (name == "datePublished")
                    {
                        if (DateTime.TryParse(tag.Attributes["content"], out DateTime date))
                        {
                            embed.PublishDate = date;
                        }
                        else
                        {
                            Trace.TraceWarning($"Enable to parse video publish date (string: {tag.Attributes["content"]})");
                        }
                    }
                    else if (name == "uploadDate")
                    {
                        if (DateTime.TryParse(tag.Attributes["content"], out DateTime date))
                        {
                            embed.UploadDate = date;
                        }
                        else
                        {
                            Trace.TraceWarning($"Enable to parse video upload date (string: {tag.Attributes["content"]})");
                        }
                    }
                    else if (name == "genre")
                    {
                        embed.Genre = tag.Attributes["content"];
                    }
                }
            }
            return embed;
        }

        #region tag parsing
        private static Tag ParseTag(ReadOnlyMemory<char> memory, ref int iterator)
        {
            bool escaped = false;
            int i = iterator;
            int start = i;
            while (i < memory.Length)
            {
                if (memory.Span[i] == '"')
                {
                    escaped = !escaped;
                }
                else if (!escaped && memory.Span[i] == '>')
                {
                    i++;
                    break;
                }
                i++;
            }
            iterator = i;

            ReadOnlyMemory<char> data = memory[start..i];
            Tag tag = new();
            for (i = 0; i < data.Length; i++)
            {
                if (data.Span[i] == ' ')
                {
                    i++;
                    break;
                }
            }

            start = i;
            for (; i < data.Length; i++)
            {
                if (data.Span[i] == '=')
                {
                    string attName = data[start..i].ToString();
                    start = (i += 2);
                    while (data.Span[i] != '"')
                    {
                        i++;
                    }
                    string attValue = data[start..i].ToString();

                    tag.Attributes.Add(attName, attValue);
                    start = (i += 2);
                }
            }

            return tag;
        }

        private static void ParseSpan(ReadOnlyMemory<char> memory, ref int iterator, List<Tag> tags)
        {
            int i = iterator;
            int start = iterator;
            bool escaped = false;

            string itemType = null;
            while (i < memory.Length)
            {
                if (memory.Span[i] == '"')
                {
                    escaped = !escaped;
                }
                else if (!escaped)
                {
                    if (memory.Span[i] == '<' && memory.Span[i + 1] == '/')
                    {
                        while (memory.Span[i] != '>')
                        {
                            i++;
                        }
                        i++;
                        break;
                    }
                    else if (itemType == null && memory.Span[i] == ' ')
                    {
                        if (IsItemProp(memory, ++i))
                        {
                            while (memory.Span[i] != '=')
                            {
                                i++;
                            }

                            i += 2;
                            int j = i;
                            while (memory.Span[i] != '"')
                            {
                                i++;
                            }
                            var value = memory[j..i];
                            itemType = value.ToString();
                        }
                    }
                }
                i++;
            }
            iterator = i;
            ReadOnlyMemory<char> span = memory[start..i];
            i = 0;
            while (span.Span[i] != '>')
            {
                i++;
            }
            i += 2;
            for (; i < span.Length; i++)
            {
                if (span.Span[i] == ' ')
                {
                    Tag tag = new();
                    tag.Attributes.Add("itemtype", itemType);
                    i++;

                    start = i;
                    for (; i < span.Length; i++)
                    {
                        if (span.Span[i] == '=')
                        {
                            string attName = span[start..i].ToString();
                            start = (i += 2);
                            while (span.Span[i] != '"')
                            {
                                i++;
                            }
                            string attValue = span[start..i].ToString();

                            tag.Attributes.Add(attName, attValue);
                            start = (++i + 1); // skip "
                        }
                        if (span.Span[i] == '>')
                        {
                            i++;
                            start = i + 1; // skip to start of the next tag
                            break;
                        }
                    }
                    tags.Add(tag);
                }
            }
        }

        private static bool IsMeta(ReadOnlyMemory<char> memory, int i)
        {
            return memory.Span[i] == 'm' && i++ < memory.Length
                && memory.Span[i] == 'e' && i++ < memory.Length
                && memory.Span[i] == 't' && i++ < memory.Length
                && memory.Span[i] == 'a';
        }

        private static bool IsSpan(ReadOnlyMemory<char> memory, int i)
        {
            return memory.Span[i] == 's' && i++ < memory.Length
                && memory.Span[i] == 'p' && i++ < memory.Length
                && memory.Span[i] == 'a' && i++ < memory.Length
                && memory.Span[i] == 'n';
        }

        private static bool IsItemProp(ReadOnlyMemory<char> memory, int i)
        {
            return memory.Span[i] == 'i' && i++ < memory.Length
                && memory.Span[i] == 't' && i++ < memory.Length
                && memory.Span[i] == 'e' && i++ < memory.Length
                && memory.Span[i] == 'm' && i++ < memory.Length
                && memory.Span[i] == 'p' && i++ < memory.Length
                && memory.Span[i] == 'r' && i++ < memory.Length
                && memory.Span[i] == 'o' && i++ < memory.Length
                && memory.Span[i] == 'p';
        }
        #endregion
    }

    internal record Tag()
    {
        public Dictionary<string, string> Attributes { get; } = new();
    }
}
