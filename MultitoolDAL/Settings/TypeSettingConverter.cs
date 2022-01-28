using System;
using System.Reflection;
using System.Xml;

namespace Multitool.DAL.Settings
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
            if (toRestore != null && toRestore.Name == "Type")
            {
                var attributes = toRestore.Attributes;
                var value = attributes["value"]?.Value;
                if (value is string typeAssemblyQualifiedName && !string.IsNullOrWhiteSpace(typeAssemblyQualifiedName))
                {
                    var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
                    for (int i = 0; i < assemblyTypes.Length; i++)
                    {
                        if (assemblyTypes[i].AssemblyQualifiedName == typeAssemblyQualifiedName)
                        {
                            return assemblyTypes[i];
                        }
                    }
                }
            }

            return null;
        }
    }
}
