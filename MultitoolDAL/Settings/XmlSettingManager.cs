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

        public bool AutoCommit { get; set; } = true;
        #endregion

        public event TypedEventHandler<ISettingsManager, string> SettingsChanged;

        #region public methods

        #region ISettingsManager
        public T GetSetting<T>(string globalKey, string settingKey)
        {
            XmlNode globalNode = settingsRootNode.SelectSingleNode(".//" + globalKey);
            if (globalNode != null)
            {
                XmlNode settingNode = globalNode.SelectSingleNode(".//" + settingKey);

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
            if (!useSettingAttribute)
            {
                throw new NotSupportedException("Function is not implemented to save all object properties.");
            }

            XmlNode values = settingsRootNode.SelectSingleNode(".//" + typeof(T).FullName);
            PropertyInfo[] props = typeof(T).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                IEnumerable<Attribute> attributes = props[i].GetCustomAttributes();
                foreach (Attribute attribute in attributes)
                {
                    if (attribute is SettingAttribute settingAttribute)
                    {
                        try
                        {
                            if (values != null)
                            {
                                // get prop name with SettingName property
                                string propName = settingAttribute.SettingName ?? props[i].Name;
                                XmlNode node = values.SelectSingleNode($".//{propName}");
                                if (node == null)
                                {
                                    SetPropertyValue(props[i], toLoad, settingAttribute);
                                }
                                else
                                {
                                    object value;
                                    if (IsList(props[i].PropertyType))
                                    {
                                        var xmlGenericType = node.Attributes["type"];
                                        Type[] generics = props[i].PropertyType.GetGenericArguments();
                                        Type genericType = generics.Length > 0 ? genericType = generics[0] : genericType = typeof(object);
                                        if (xmlGenericType != null && xmlGenericType.Value != genericType.FullName)
                                        {
                                            throw new ArrayTypeMismatchException();
                                        }

                                        var childNodes = node.ChildNodes;
                                        IList list = (IList)GetTypeDefaultValue(props[i].PropertyType);
                                        if (settingAttribute.Converter == null)
                                        {
                                            foreach (XmlNode childNode in childNodes)
                                            {
                                                list.Add(Convert.ChangeType(childNode.InnerText, genericType));
                                            }
                                        }
                                        else
                                        {
                                            foreach (XmlNode childNode in childNodes)
                                            {
                                                object restored = settingAttribute.Converter.Restore(childNode);
                                                list.Add(restored);
                                            }
                                        }
                                        
                                        value = list;
                                    }
                                    else
                                    {
                                        if (settingAttribute.Converter != null)
                                        {
                                            value = settingAttribute.Converter.Restore(node);
                                        }
                                        else
                                        {
                                            XmlAttribute xmlAttribute = node.Attributes["value"];
                                            value = xmlAttribute != null ? xmlAttribute.Value : node.InnerText;
                                        }
                                    }
                                    SetPropertyValue(props[i], toLoad, settingAttribute, value);
                                }
                            }
                            else
                            {
                                SetPropertyValue(props[i], toLoad, settingAttribute);
                            }
                        }
                        catch (TargetException ex)
                        {
                            Trace.TraceError($"Failed to load {typeof(T).Name}.{props[i].Name} :\n{ex}");
                        }
                        catch (TargetInvocationException ex)
                        {
                            Trace.TraceError($"Failed to load {typeof(T).Name}.{props[i].Name} :\n{ex}");
                        }
                        break;
                    }
                }
            }
        }

        public void Save<T>(T toSave, bool useSettingAttribute = true)
        {
            if (toSave is null)
            {
                throw new ArgumentNullException(nameof(toSave));
            }
            if (!useSettingAttribute)
            {
                throw new NotSupportedException("Function is not implemented to save all object properties.");
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
            else
            {
                XmlAttribute attribute = rootNode.Attributes["timestamp"];
                if (attribute != null)
                {
                    attribute.Value = DateTime.Now.ToUniversalTime().ToLongTimeString();
                }
                else
                {
                    attribute = document.CreateAttribute("timestamp");
                    attribute.Value = DateTime.Now.ToUniversalTime().ToLongTimeString();
                    rootNode.Attributes.Append(attribute);
                }
            }

            PropertyInfo[] props = typeof(T).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                IEnumerable<Attribute> attributes = props[i].GetCustomAttributes();

                foreach (Attribute attribute in attributes)
                {
                    if (attribute is SettingAttribute settingAttribute)
                    {
                        try
                        {
                            object propValue = props[i].GetValue(toSave);
                            if (propValue != null)
                            {
                                XmlNode settingNode;
                                string settingName = settingAttribute.SettingName ?? props[i].Name;
                                XmlNode previousNode = rootNode.SelectSingleNode($".//{settingName}");
                                if (previousNode != null)
                                {
                                    rootNode.RemoveChild(previousNode);
                                }
                                settingNode = document.CreateElement(settingAttribute.SettingName ?? props[i].Name);

                                if (settingAttribute.Converter != null)
                                {
                                    if (IsList(props[i].PropertyType))
                                    {
                                        var list = (IList)propValue;
                                        foreach (var item in list)
                                        {
                                            settingNode.AppendChild(document.ImportNode(settingAttribute.Converter.Convert(item), true));
                                        }
                                    }
                                    else
                                    {
                                        XmlElement xml = (XmlElement)settingAttribute.Converter.Convert(propValue);
                                        settingNode.AppendChild(xml);
                                    }
                                }
                                else // auto convert
                                {
                                    if (props[i].PropertyType.IsPrimitive || props[i].PropertyType == typeof(string))
                                    {
                                        XmlAttribute valueAttribute = document.CreateAttribute("value");
                                        valueAttribute.Value = propValue.ToString();
                                        settingNode.Attributes.Append(valueAttribute);
                                    }
                                    else
                                    {
                                        if (IsList(props[i].PropertyType))
                                        {
                                            // can crash with casting exception
                                            FlattenList(settingNode, (IList)propValue, props[i].PropertyType);
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"Cannot save {props[i].DeclaringType}.{props[i].Name}. It is neither a primitive type (string included) or a list and it does not have a custom converter.");
                                        }
                                    }
                                }

                                rootNode.AppendChild(settingNode);
                            }
#if DEBUG
                            else
                            {
                                Trace.TraceWarning($"Not saving {props[i].Name}, property value is null");
                            }
#endif
                        }
                        catch (TargetException ex)
                        {
                            Trace.TraceError($"Failed to save {typeof(T).Name}.{props[i].Name} :\n{ex}");
                        }
                        catch (TargetInvocationException ex)
                        {
                            Trace.TraceError($"Failed to save {typeof(T).Name}.{props[i].Name} :\n{ex}");
                        }
                    }
                }
            }

            settingsRootNode.AppendChild(rootNode);
            if (AutoCommit)
            {
                document.Save(filePath);
            }
        }

        public void SaveSetting(string callerName, string name, object value)
        {
            if (value == null)
            {
                Trace.TraceWarning($"Not saving {callerName}/{name}, value is null");
            }

            XmlNode node = settingsRootNode.SelectSingleNode($".//{callerName}");
            if (node != null)
            {
                XmlNode settingNode = node.SelectSingleNode($".//{name}");
                if (settingNode == null)
                {
                    XmlNode toSave = document.CreateElement(name);
                    node.AppendChild(toSave);
                    toSave.InnerText = value.ToString();
                }
                else
                {
                    settingNode.InnerText = value.ToString();
                }
            }
            else
            {
                node = document.CreateElement(callerName);
                XmlNode toSave = document.CreateElement(name);
                toSave.InnerText = value.ToString();

                node.AppendChild(toSave);
                settingsRootNode.AppendChild(node);
            }

            if (AutoCommit)
            {
                document.Save(filePath);
            }
        }

        public object TryGetSetting(string globalKey, string settingKey)
        {
            XmlNode globalNode = settingsRootNode.SelectSingleNode(".//" + globalKey);
            if (globalNode != null)
            {
                XmlNode settingNode = globalNode.SelectSingleNode(".//" + settingKey);
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

        public void Commit()
        {
            // TODO
            document.Save(filePath);
        }

        public static async Task<XmlSettingManager> Get()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("settings.xml", CreationCollisionOption.OpenIfExists);
            return new();
        }
        #endregion

        #region private methods
        private void SetPropertyValue<T>(PropertyInfo prop, T toLoad, SettingAttribute settingAttribute, object value = null)
        {
            if (prop.CanWrite)
            {
                if (value == null)
                {
                    if (settingAttribute.HasDefaultValue)
                    {
                        prop.SetValue(toLoad, settingAttribute.DefaultValue);
                    }
                    else if (settingAttribute.DefaultInstanciate)
                    {
                        prop.SetValue(toLoad, GetTypeDefaultValue(prop.PropertyType));
                    }
                }
                else
                {
                    prop.SetValue(toLoad, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            else
            {
#if DEBUG
                Trace.TraceWarning($"Cannot set {typeof(T).Name}.{prop.Name}, property is readonly.");
#else
                throw new TargetException($"Cannot set {typeof(T).Name}.{prop.Name}, property is readonly.");
#endif
            }
        }

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

            XmlAttribute genericAttribute = document.CreateAttribute("type");
            genericAttribute.Value = genericType.FullName;
            parentNode.Attributes.Append(genericAttribute);

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

        private static object GetTypeDefaultValue(Type type)
        {
            ConstructorInfo ctorInfo = type.GetConstructor(Array.Empty<Type>());
            if (ctorInfo == null)
            {
                return null;
            }
            else
            {
                object value = null;
                try
                {
                    value = Convert.ChangeType(ctorInfo.Invoke(Array.Empty<object>()), type);
                }
                catch (InvalidCastException ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                return value;
            }
        }
        #endregion
    }
}
