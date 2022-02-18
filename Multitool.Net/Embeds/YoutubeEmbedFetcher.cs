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

using Windows.Web.Http;

namespace Multitool.Net.Embeds
{
    public class YoutubeEmbedFetcher : IEmbedFetcher, IDisposable
    {
        private static readonly Regex youtubeRegex = new(@"^(https|http):\/\/www.youtube.com\/watch\?.+");
        private readonly HttpClient client = new();

        public void Dispose()
        {
            client.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<object> Fetch(string url)
        {
            if (youtubeRegex.IsMatch(url))
            {
                var response = await client.GetAsync(new(url));
                response.EnsureSuccessStatusCode();

                string raw = await response.Content.ReadAsStringAsync();
                //JsonSerializer.Deserialize()
                ReadOnlyMemory<char> memory = new(raw.ToCharArray());
                SearchHead(memory, out int beginning, out int end);
                ReadOnlyMemory<char> head = memory[beginning..end];
                string headAsString = head.ToString();

                XmlDocument doc = new();
                doc.LoadXml(headAsString);
                Debug.WriteLine($"Parsed html document, {doc.ChildNodes.Count} nodes.");
            }
            else
            {
                throw new FormatException($"Uri does not match the expected format (expected: {youtubeRegex})");
            }

            return null;
        }

        private bool IsHead(ReadOnlyMemory<char> memory, ref int i)
        {
            return memory.Span[i] == 'h' && 
                i++ < memory.Length && memory.Span[i] == 'e' &&
                i++ < memory.Length && memory.Span[i] == 'a' &&
                i++ < memory.Length && memory.Span[i] == 'd';
        }

        private void SearchHead(ReadOnlyMemory<char> memory, out int beginning, out int end)
        {
#if DEBUG
            nint iterations = 0;
#endif
            int i = 0;
            beginning = 0;
            end = -1;
            while (i < memory.Length)
            {
                if (memory.Span[i] == '<') // opening tag
                {
                    if ((i + 1) < memory.Length)
                    {
                        int j = i + 1;
                        char c = memory.Span[j];
                        if (IsHead(memory, ref j))
                        {
                            beginning = i;
                            i = j + 1;
                            break;
                        }
                        else
                        {
                            // we need to skip the tag
                        }
                        i = j + 1; // assuming the tag is well formed
                    }
                }
                else if (memory.Span[i] == '&')
                {
                    memory.Span[i] = 0;
                }
                i++;
                iterations++;
            }
            while (i < memory.Length)
            {
                if (memory.Span[i] == '<' && i++ < memory.Length && memory.Span[i] == '/') // closing tag
                {
                    i++;
                    if (IsHead(memory, ref i))
                    {
                        end = i + 2;
                        break;
                    }
                    else
                    {
                        // we need to skip the tag
                    }
                }
                i++;
                iterations++;
            }

            Debug.WriteLine($"Iterations: {iterations} for {memory.Length} chars.\nRange: [{beginning},{end}]");
        }
    }
}
