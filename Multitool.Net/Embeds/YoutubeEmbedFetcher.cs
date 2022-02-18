using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Embeds
{
    public class YoutubeEmbedFetcher : IEmbedFetcher
    {
        private static readonly Regex youtubeRegex = new(@"^https://youtube\.[a-z]+");
        private readonly HttpClient client = new();

        public async Task<object> Fetch(string url)
        {
            if (youtubeRegex.IsMatch(url))
            {
                var response = await client.GetAsync(new(url));
                response.EnsureSuccessStatusCode();
                
            }
            return null;
        }
    }
}
