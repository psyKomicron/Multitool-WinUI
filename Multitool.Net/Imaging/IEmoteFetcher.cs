using System.Collections.Generic;
using System.Threading.Tasks;

namespace Multitool.Net.Imaging
{
    public interface IEmoteFetcher
    {
        ImageSize DefaultImageSize { get; set; }

        Task<List<string>> ListChannelEmotes(string channel);
        Task<List<Emote>> FetchChannelEmotes(string channel);
        Task<List<Emote>> FetchGlobalEmotes();
    }
}