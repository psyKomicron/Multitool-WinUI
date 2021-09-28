using System;
using System.Diagnostics;

using Windows.Foundation.Collections;
using Windows.Storage;

namespace Multitool.DAL
{
    public class Settings : ISettings
    {
        public Settings(ApplicationDataContainer container)
        {
            DataContainer = container;
        }

        /// <inheritdoc/>
        public ApplicationDataContainer DataContainer { get; }

        /// <inheritdoc/>
        public string SettingFormat { get; set; }

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
                set.Add(actualKey, value);
            }
            Trace.TraceInformation("Saved '" + actualKey + "'");
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

    }
}
