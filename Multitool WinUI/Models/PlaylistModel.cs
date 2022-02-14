using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MultitoolWinUI.Models
{
    public class PlaylistModel
    {
        public PlaylistModel()
        {
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Songs { get; set; }
    }
}
