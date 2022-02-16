using Multitool.Data.Media;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MultitoolWinUI.Models
{
    public class PlaylistModel
    {
        public PlaylistModel(Playlist playlist)
        {
            Playlist = playlist;
        }

        public string Name => Playlist.Name;
        public string Description => Playlist.Description;
        public Playlist Playlist { get; private set; }
    }
}
