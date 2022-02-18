using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Net.Embeds
{
    public interface IEmbedFetcher
    {
        Task<object> Fetch(string url);
    }
}
