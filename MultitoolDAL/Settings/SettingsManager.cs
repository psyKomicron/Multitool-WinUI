using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Multitool.DAL.Settings
{
    public class SettingsManager : ISettingsManager
    {
        //private 

        public SettingsManager(ApplicationDataContainer container)
        {
            DataContainer = container;
            ApplicationData.Current.DataChanged += ApplicationData_DataChanged;
        }

        #region events
        public event TypedEventHandler<ISettingsManager, string> SettingsChanged;
        #endregion

        #region properties
        /// <inheritdoc/>
        public ApplicationDataContainer DataContainer { get; }

        /// <summary>
        /// How the setting key will be created
        /// </summary>
        public string SettingFormat { get; set; }
        #endregion

        #region public methods
        /// <inheritdoc/>
        public void Save<T>(T toSave, bool useSettingAttribute = true)
        {
            if (toSave is null)
            {
                throw new ArgumentNullException(nameof(toSave));
            }

            if (useSettingAttribute)
            {
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
                                object value = propertyInfos[i].GetValue(toSave);
                                if (settingAttribute.Converter != null)
                                {
                                    value = settingAttribute.Converter.Convert(value);
                                }
                                SaveSetting(typeof(T).Name, propertyInfos[i].Name, value);
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
            }
        }

        /// <inheritdoc/>
        public void Load<T>(T toLoad, bool useSettingAttribute = true)
        {
            if (useSettingAttribute)
            {
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
                                if (TryGetSetting(props[i].PropertyType, typeof(T).Name, props[i].Name, out object value))
                                {
                                    if (settingAttribute.Converter != null)
                                    {
                                        props[i].SetValue(toLoad, settingAttribute.Converter.Restore(value));
                                    }
                                    else
                                    {
                                        props[i].SetValue(toLoad, value);
                                    }
                                }
                                else
                                {
                                    Trace.TraceWarning($"Setting not found ({string.Format(SettingFormat, typeof(T).Name, props[i].Name)}). Loading default value.");

                                    if (settingAttribute.HasDefaultValue)
                                    {
                                        props[i].SetValue(toLoad, settingAttribute.DefaultValue);
                                    }
                                    else if (settingAttribute.WantsDefaultValue)
                                    {
                                        if (settingAttribute.Converter != null)
                                        {
                                            props[i].SetValue(toLoad, settingAttribute.Converter.Restore(settingAttribute.DefaultValue));
                                        }
                                        else
                                        {
                                            props[i].SetValue(toLoad, GetTypeDefaultValue(props[i].PropertyType));
                                        }
                                    }
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
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void SaveSetting(string callerName, string name, object value)
        {
            string actualKey = string.Format(SettingFormat, callerName, name);
            IPropertySet set = DataContainer.Values;
            if (set.ContainsKey(actualKey))
            {
                set[actualKey] = value;
            }
            else
            {
                try
                {
                    set.Add(actualKey, value);
                }
                catch (ArgumentException ex)
                {
                    Trace.TraceError($"Failed to save '{actualKey}', system was not able to serialize {value.GetType().FullName}\n{ex}");
                    return;
                }
            }
            SettingsChanged?.Invoke(this, actualKey);
            Trace.TraceInformation($"Saved '{actualKey}'");
        }

        /// <inheritdoc/>
        public T GetSetting<T>(string callerName, string name)
        {
            IPropertySet set = DataContainer.Values;
            string key = string.Format(SettingFormat, callerName, name);
            if (set.TryGetValue(key, out object value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                throw new SettingNotFoundException(key);
            }
        }

#nullable enable
        /// <inheritdoc/>
        public object? TryGetSetting(string callerName, string name)
        {
            IPropertySet set = DataContainer.Values;
            string key = string.Format(SettingFormat, callerName, name);
            return set.TryGetValue(key, out object? value) ? value : null;
        }
#nullable disable

        /// <inheritdoc/>
        public bool TryGetSetting(Type settingType, string callerName, string name, out object value)
        {
            IPropertySet set = DataContainer.Values;
            string key = string.Format(SettingFormat, callerName, name);
            if (set.TryGetValue(key, out object setting))
            {
                try
                {
                    value = Convert.ChangeType(setting, settingType);
                    return true;
                }
                catch (InvalidCastException ex)
                {
                    Trace.TraceError(ex.ToString());

                    value = null;
                    return false;
                }
                catch (FormatException ex) 
                {
                    Trace.TraceError(ex.ToString());

                    value = null;
                    return false;
                }
            }
            else
            {
                value = null;
                return false;
            }
        }
#endregion

        #region private
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

        #region event handlers
        private void ApplicationData_DataChanged(ApplicationData sender, object args)
        {
            
        }
        #endregion

        #endregion
    }
}
