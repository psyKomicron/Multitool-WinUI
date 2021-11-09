using System;
using System.Collections.Generic;
using System.Diagnostics;

using Windows.Foundation.Collections;
using Windows.Storage;

namespace Multitool.DAL
{
    public class Settings : ISettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Settings() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="container">The <see cref="ApplicationDataContainer"/> to save and get settings from</param>
        public Settings(ApplicationDataContainer container)
        {
            DataContainer = container;
        }

        /// <inheritdoc/>
        public ApplicationDataContainer DataContainer { get; init; }

        /// <inheritdoc/>
        public string SettingFormat { get; set; }

        /// <inheritdoc/>
        public void SaveSetting(string callerName, string name, object value)
        {
            string actualKey = string.Format(SettingFormat, callerName, name);
            if (!CheckKey(actualKey, out Exception ex))
            {
                throw ex;
            }

            IPropertySet set = DataContainer.Values;
            if (set.ContainsKey(actualKey))
            {
                set[actualKey] = value;
                Trace.TraceInformation($"Saved \"{actualKey}\"");
            }
            else
            {
                set.Add(actualKey, value);
                Trace.TraceInformation($"Created and saved \"{actualKey}\"");
            }
        }

        /// <inheritdoc/>
        public void SaveSetting<TIn, TOut>(string settingName, ISettingConverter<TIn, TOut> converter, TIn value)
        {
            if (!CheckKey(settingName, out Exception ex))
            {
                throw ex;
            }

            IPropertySet set = DataContainer.Values;
            if (set.ContainsKey(settingName))
            {
                set[settingName] = converter.Convert(value);
                Trace.TraceInformation($"Saved \"{settingName}\"");
            }
            else
            {
                set.Add(settingName, converter.Convert(value));
                Trace.TraceInformation($"Created and saved \"{settingName}\"");
            }
        }

        /// <inheritdoc/>
        public T GetSetting<T>(string callerName, string name)
        {
            string key = string.Format(SettingFormat, callerName, name);
            if (!CheckKey(key, out Exception exception))
            {
                throw exception;
            }

            IPropertySet set = DataContainer.Values;
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
        public Dictionary<string, object> GetAllSettings()
        {
            return new(DataContainer.Values);
        }

        private bool CheckKey(string key, out Exception ex)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                ex = new ArgumentException("Setting key was null or empty after formatting. Setting key cannot be null or empty to be used by the settings");
                return false;
            }
            else
            {
                ex = null;
                return true;
            }
        }
    }
}
