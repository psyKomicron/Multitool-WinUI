using System.Collections.Generic;
using System.Threading.Tasks;

namespace Multitool.Net.Imaging
{
    /// <summary>
    /// Defines behavior for classes fetching links from various emote providers.
    /// </summary>
    public interface IEmoteFetcher
    {
        string Provider { get; }

        /// <summary>
        /// Fetches emotes download links from the implementation's provider for the given channel.
        /// </summary>
        /// <param name="channel">Channel to fetch the emotes from.</param>
        /// <returns>List of image download links.</returns>
        Task<Emote[]> FetchChannelEmotes(string channel);
        Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except);

        /// <summary>
        /// Fetches emotes download links from the implementation's provider.
        /// </summary>
        /// <returns>List of image download links.</returns>
        Task<Emote[]> FetchGlobalEmotes();        
    }
}