using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Embeds
{
    public class TwitchEmbedFetcher : IEmbedFetcher, IDisposable
    {
        // https://www.twitch.tv/pokelawls/clip/LivelyPoisedGuanacoM4xHeh-7pKL-x5VGBgLTi_S
        private static readonly Regex validUrlRegex = new(@"^(https|http)://clips\.twitch\.tv/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly HttpClient client = new();

        public bool CanFetch(string url)
        {
            return validUrlRegex.IsMatch(url);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<Embed> Fetch(string url)
        {
            if (validUrlRegex.IsMatch(url))
            {
                using HttpResponseMessage response = await client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                string raw = await response.Content.ReadAsStringAsync();

                _ = EmbedFetcherHelper.DumpRaw(raw, true);

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
                            else
                            {
                                // we need to skip the tag & content
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
                _ = EmbedFetcherHelper.DumpTags(tags);

                var embed = CreateEmbed(tags);
                return embed;
            }
            else
            {
                throw new FormatException($"Url does not match the expected format ({validUrlRegex})");
            }

            throw new NotImplementedException();
        }

        private static VideoEmbed CreateEmbed(List<Tag> tags)
        {
            VideoEmbed embed = new();
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag.Attributes.TryGetValue("name", out string name))
                {
                    if (name == "title")
                    {
                        embed.Title = tag.Attributes["content"];
                    }
                }
                else if (tag.Attributes.TryGetValue("property", out name))
                {
                    if (name == "og:image")
                    {
                        embed.ThumbnailUrl = new(tag.Attributes["content"]);
                    }
                    else if (name == "og:video:duration")
                    {
                        embed.Duration = TimeSpan.FromSeconds(double.Parse(tag.Attributes["content"]));
                    }
                    else if (name == "og:video:release_date")
                    {
                        if (DateTime.TryParse(tag.Attributes["content"], out DateTime releaseDate))
                        {
                            embed.UploadDate = releaseDate;
                        }
                    }
                    else if (name == "og:url")
                    {
                        embed.Url = new(tag.Attributes["content"]);
                    }
                }
            }
            return embed;
        }

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

        private static bool IsMeta(ReadOnlyMemory<char> memory, int i)
        {
            return memory.Span[i] == 'm' && i++ < memory.Length
                && memory.Span[i] == 'e' && i++ < memory.Length
                && memory.Span[i] == 't' && i++ < memory.Length
                && memory.Span[i] == 'a';
        }
    }
}
