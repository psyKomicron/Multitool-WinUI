using System.Threading.Tasks;

namespace Multitool.Net.Embeds
{
    public interface IEmbedFetcher
    {
        bool CanFetch(string url);
        Task<Embed> Fetch(string url);
    }
}
