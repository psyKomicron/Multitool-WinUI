using System;
using System.Reflection;
using System.Xml;

namespace Multitool.DAL.Settings.Converters
{
    public class TypeSettingConverter : ISettingConverter
    {
        public XmlNode Convert(object toConvert)
        {
            if (toConvert is Type t)
            {
                XmlDocument doc = new();
                XmlNode node = doc.CreateElement("Type");
                XmlAttribute attribute = doc.CreateAttribute("value");
                attribute.Value = t.AssemblyQualifiedName;

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
            if (toRestore != null && toRestore.HasChildNodes && toRestore.FirstChild.Name == "Type")
            {
                var attributes = toRestore.FirstChild.Attributes;
                var value = attributes["value"]?.Value;
                if (value is string typeAssemblyQualifiedName && !string.IsNullOrWhiteSpace(typeAssemblyQualifiedName))
                {
                    var loadedType = Type.GetType(typeAssemblyQualifiedName);
                    if (loadedType != null)
                    {
                        return loadedType;
                    }
                }
            }
            return null;
        }

        public object Restore(object defaultValue)
        {
            if (defaultValue is string typeAssemblyQualifiedName && !string.IsNullOrWhiteSpace(typeAssemblyQualifiedName))
            {
                var loadedType = Type.GetType(typeAssemblyQualifiedName);
                if (loadedType != null)
                {
                    return loadedType;
                }
            }
            return null;
        }
    }
}
