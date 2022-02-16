using Multitool.Data.Settings;
using Multitool.Data.Settings.Converters;

using MultitoolWinUI.Models;

using System.Xml;

namespace MultitoolWinUI.Helpers
{
    internal class PathHistoryItemSettingConverter : ISettingConverter
    {
        public XmlNode Convert(object toConvert)
        {
            if (toConvert is PathHistoryItem item)
            {
                XmlDocument doc = new();
                string fullPath = item.FullPath;
                string shortPath = item.ShortPath;

                XmlElement node = doc.CreateElement(typeof(PathHistoryItem).Name);
                XmlAttribute attribute = doc.CreateAttribute("fullpath");
                attribute.Value = fullPath;
                node.Attributes.Append(attribute);
                attribute = doc.CreateAttribute("shortpath");
                attribute.Value = shortPath;
                node.Attributes.Append(attribute);
                return node;
            }
            else
            {
                return null;
            }
        }

        public object Restore(XmlNode toRestore)
        {
            XmlAttribute fullPath = toRestore.Attributes["fullpath"];
            XmlAttribute shortPath = toRestore.Attributes["shortpath"];
            if (fullPath != null && shortPath != null)
            {
                return new PathHistoryItem()
                {
                    FullPath = fullPath.Value.ToString(),
                    ShortPath = shortPath.Value.ToString()
                };
            }
            return null;
        }

        public object Restore(object defaultValue)
        {
            throw new System.NotImplementedException();
        }
    }
}
