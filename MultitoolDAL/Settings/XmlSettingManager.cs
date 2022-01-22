using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Windows.Foundation;
using Windows.Storage;

namespace Multitool.DAL.Settings
{
    public class XmlSettingManager : ISettingsManager
    {
        private readonly string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.xml");
        private readonly XmlDocument document;
        private readonly XmlNode settingsRootNode;

        public XmlSettingManager()
        {
            if (File.Exists(filePath))
            {
                document = new();
                try
                {
                    document.Load(filePath);
                }
                catch (XmlException ex)
                {
                    Trace.TraceError("Failed to load XML setting file (settings.xml)\n" + ex.ToString());
                }

                settingsRootNode = document.SelectSingleNode(".//Settings");
                if (settingsRootNode == null)
                {
                    XmlNode node = document.CreateElement("Settings");
                    document.AppendChild(node);
                    settingsRootNode = node;
                }
            }
            else
            {
                throw new FileNotFoundException($"Setting file was not found in application local folder ('{filePath}')");
            }
        }

        #region properties
        public ApplicationDataContainer DataContainer { get; init; }

        public string SettingFormat { get; set; }
        #endregion

        public event TypedEventHandler<ISettingsManager, string> SettingsChanged;

        #region public methods
        public T GetSetting<T>(string globalKey, string settingKey)
        {
            XmlNode globalNode = settingsRootNode.SelectSingleNode(".//" + globalKey);
            if (globalNode != null)
            {
                XmlNode settingNode = globalNode.SelectSingleNode("//" + settingKey);

                if (settingNode != null)
                {
                    object value = GetValueFromLeaf(settingNode);
                    if (value != null)
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    else
                    {
                        return default;
                    }
                }
                else
                {
                    throw new SettingNotFoundException(settingKey);
                }
            }
            else
            {
                throw new SettingNotFoundException(globalKey, "Root node was not found");
            }
        }

        public void Load<T>(T toLoad, bool useSettingAttribute = true)
        {
#if !DEBUG
            throw new NotImplementedException(); 
#endif
        }

        public void Save<T>(T toSave, bool useSettingAttribute = true)
        {
            if (toSave is null)
            {
                throw new ArgumentNullException(nameof(toSave));
            }
            if (!useSettingAttribute)
            {
                throw new NotImplementedException("Function is not implemented to save all object properties.");
            }

            XmlNode rootNode = settingsRootNode.SelectSingleNode(".//" + typeof(T).FullName);
            if (rootNode == null)
            {
                rootNode = document.CreateElement(typeof(T).FullName);
                XmlAttribute attribute = document.CreateAttribute("timestamp");
                attribute.Value = DateTime.Now.ToUniversalTime().ToLongTimeString();
                rootNode.Attributes.Append(attribute);
                settingsRootNode.AppendChild(rootNode);
            }

            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            for (int i = 0; i < propertyInfos.Length; i++)
            {
                IEnumerable<Attribute> attributes = propertyInfos[i].GetCustomAttributes();

                foreach (Attribute attribute in attributes)
                {
                    if (attribute is SettingAttribute settingAttribute)
                    {
                        try
                        {
                            object propValue = propertyInfos[i].GetValue(toSave);
                            if (propValue != null)
                            {
                                XmlNode settingNode = document.CreateElement(propertyInfos[i].Name);

                                if (settingAttribute.Converter != null)
                                {
                                    propValue = settingAttribute.Converter.Convert(propValue);

                                    if (settingAttribute.Converter.IsSingleLineValue)
                                    {
                                        XmlAttribute valueAttribute = document.CreateAttribute("value");
                                        valueAttribute.Value = propValue.ToString();
                                        settingNode.Attributes.Append(valueAttribute);
                                    }
                                    else
                                    {
                                        settingNode.InnerText = propValue.ToString();
                                    }
                                }
                                else // auto convert
                                {
                                    if (propertyInfos[i].PropertyType.IsPrimitive || propertyInfos[i].PropertyType == typeof(string))
                                    {
                                        XmlAttribute valueAttribute = document.CreateAttribute("value");
                                        valueAttribute.Value = propValue.ToString();
                                        settingNode.Attributes.Append(valueAttribute);
                                    }
                                    else
                                    {
                                        if (IsList(propertyInfos[i].PropertyType))
                                        {
                                            // can crash with casting exception
                                            FlattenList(settingNode, (IList)propValue, propertyInfos[i].PropertyType);
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"Cannot save {propertyInfos[i].DeclaringType}.{propertyInfos[i].Name}. It is neither a primitive type (string included) or a list and it does not have a custom converter.");
                                        }
                                    }
                                }

                                rootNode.AppendChild(settingNode); 
                            }
#if DEBUG
                            else
                            {
                                Trace.TraceWarning($"Not saving {propertyInfos[i].Name}, property value is null");
                            } 
#endif
                        }
                        catch (TargetException ex)
                        {
                            Trace.TraceError($"Failed to save {typeof(T).Name}.{propertyInfos[i].Name} :\n{ex}");
                        }
                        catch (TargetInvocationException ex)
                        {
                            Trace.TraceError($"Failed to save {typeof(T).Name}.{propertyInfos[i].Name} :\n{ex}");
                        }
                    }
                }
            }

            settingsRootNode.AppendChild(rootNode);
            document.Save(filePath);
        }

        public void SaveSetting(string callerName, string name, object value)
        {
#if !DEBUG
            throw new NotImplementedException(); 
#endif
        }

        public object TryGetSetting(string globalKey, string settingKey)
        {
            XmlNode globalNode = settingsRootNode.SelectSingleNode(".//" + globalKey);
            if (globalNode != null)
            {
                XmlNode settingNode = globalNode.SelectSingleNode("//" + settingKey);
                if (settingNode != null)
                {
                    return GetValueFromLeaf(settingNode);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public bool TryGetSetting(Type settingType, string callerName, string name, out object value)
        {
            XmlNode globalNode = settingsRootNode.SelectSingleNode(".//" + callerName);
            value = null;
            if (globalNode != null)
            {
                XmlNode settingNode = globalNode.SelectSingleNode("//" + name);

                if (settingNode != null)
                {
                    
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region private methods
        private static object GetValueFromLeaf(XmlNode settingNode)
        {
            if (settingNode.Attributes != null)
            {
                XmlAttributeCollection attributes = settingNode.Attributes;
                
                for (int i = 0; i < attributes.Count; i++)
                {
                    if (attributes[i].Name == "value")
                    {
                        return attributes[i].Value;
                    }
                }
            }
            
            if (settingNode.FirstChild != null)
            {
                return settingNode.FirstChild.InnerText;
            }
            else
            {
                return null;
            }
        }

        private void FlattenList(XmlNode parentNode, IList list, Type propType)
        {
            Type genericType;
            Type[] generics = propType.GetGenericArguments();
            if (generics.Length > 0)
            {
                genericType = generics[0];
            }
            else
            {
                genericType = typeof(object);
            }

            foreach (var element in list)
            {
                XmlNode elementNode = document.CreateElement(genericType.Name);
                elementNode.InnerText = element.ToString();
                parentNode.AppendChild(elementNode);
            }
        }

        private static bool IsList(Type t)
        {
            Type[] interfaces = t.FindInterfaces((Type m, object filter) => m == typeof(IList), null);
            return interfaces.Length > 0;
        }
        #endregion
    }
}
