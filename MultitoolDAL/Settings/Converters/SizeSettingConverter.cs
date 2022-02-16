using System;
using System.Xml;

using Windows.Foundation;

namespace Multitool.Data.Settings.Converters
{
    public class SizeSettingConverter : ISettingConverter
    {
        public XmlNode Convert(object toConvert)
        {
            if (toConvert is Size size)
            {
                XmlDocument xmlDoc = new();
                var node = xmlDoc.CreateElement("Size");

                var widthAttr = xmlDoc.CreateAttribute("Width");
                var heightAttr = xmlDoc.CreateAttribute("Height");
                widthAttr.Value = size.Width.ToString();
                heightAttr.Value = size.Height.ToString();

                node.Attributes.Append(widthAttr);
                node.Attributes.Append(heightAttr);
                return node;
            }
            return null;
        }

        public object Restore(XmlNode toRestore)
        {
            var node = toRestore.SelectSingleNode($"./Size");
            if (node != null)
            {
                var widthAttr = node.Attributes["Width"];
                var heightAttr = node.Attributes["Height"];
                if (heightAttr != null && widthAttr != null)
                {
                    if (double.TryParse(heightAttr.Value, out double height) && double.TryParse(widthAttr.Value, out double width))
                    {
                        Size size = new(width, height);
                        return size;
                    }
                } 
            }
            return null;
        }

        public object Restore(object defaultValue)
        {
            if (defaultValue is object[] parameters && parameters.Length == 2)
            {
                if (double.TryParse(parameters[0].ToString(), out double width) && double.TryParse(parameters[1].ToString(), out double height))
                {
                    Size size = new(width, height);
                    return size;
                }
            }
            return null;
        }
    }
}
