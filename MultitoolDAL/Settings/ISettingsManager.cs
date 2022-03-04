using System.Collections.Generic;

using Windows.Foundation;

namespace Multitool.Data.Settings
{
    public interface ISettingsManager
    {
        void Edit(string globalKey, string settingKey, object value);
        T Get<T>(string globalKey, string settingKey);
        List<string> ListKeys();
        List<string> ListKeys(string globalKey);
        void Remove(string globalKey, string settingKey);
        /// <summary>
        /// Deletes all entries managed by this <see cref="ISettingsManager"/>
        /// </summary>
        void Reset();
        /// <summary>
        /// Saves a value into the implementation's medium.
        /// </summary>
        /// <remarks>
        /// A setting is the combination of 2 <see cref="string"/> to create it's unique name, and a value.
        /// </remarks>
        /// <param name="globalKey"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void Save(string globalKey, string name, object value);
        bool TryGet<T>(string globalKey, string name, out T value);
        object TryGet(string globalKey, string settingKey);

        event TypedEventHandler<IUserSettingsManager, string> SettingsChanged;
    }
}