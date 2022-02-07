using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace Multitool.DAL.Settings.Converters
{
    public class RegexSettingConverter : ISettingConverter
    {
        public XmlNode Convert(object toConvert)
        {
            if (toConvert is Regex regex)
            {
                string regexString = regex.ToString();
                RegexOptions options = regex.Options;

                XmlDocument doc = new();
                var node = doc.CreateElement("Regex");

                XmlAttribute attribute = doc.CreateAttribute("converter");
                attribute.Value = nameof(RegexSettingConverter);
                node.Attributes.Append(attribute);

                attribute = doc.CreateAttribute("regex");
                attribute.Value = regexString;
                node.Attributes.Append(attribute);

                if (!options.HasFlag(RegexOptions.None))
                {
                    attribute = doc.CreateAttribute("options");
                    attribute.Value = ((float)options).ToString();
                    node.Attributes.Append(attribute);
                }
                return node;
            }
            return null;
        }

        public object Restore(XmlNode toRestore)
        {
            if (toRestore != null && toRestore.Name == "Regex" && toRestore.Attributes != null)
            {
                XmlAttribute options = toRestore.Attributes["options"];
                XmlAttribute regex = toRestore.Attributes["regex"];
                if (regex != null)
                {
                    Regex actual;
                    if (options != null && float.TryParse(options.Value, out float enumValue))
                    {
                        actual = new(regex.Value, (RegexOptions)enumValue);
                    }
                    else
                    {
                        actual = new(regex.Value);
                    }
                    return actual;
                }
            }
            return null;
        }

        public object Restore(object defaultValue)
        {
            if (defaultValue is string s && !string.IsNullOrEmpty(s))
            {
                return new Regex(s);
            }
            return null;
        }
    }
}
