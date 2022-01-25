using System;

using Windows.Foundation;
using Windows.Storage;

namespace Multitool.DAL.Settings
{
    /// <summary>
    /// Defines behavior for classes handling app settings.
    /// </summary>
    public interface ISettingsManager
    {   
        event TypedEventHandler<ISettingsManager, string> SettingsChanged;

        /// <summary>
        /// Loads values into the corresponding properties of <paramref name="toLoad"/> from the 
        /// <see cref="ApplicationDataContainer"/>.
        /// </summary>
        /// <typeparam name="T">Generic for the class</typeparam>
        /// <param name="toLoad">Instance to load</param>
        /// <param name="useSettingAttribute">
        /// <see langword="true"/> to save only properties with a <see cref="SettingAttribute"/>,
        /// <see langword="false"/> to save all properties of <paramref name="toLoad"/>
        /// </param>
        void Load<T>(T toLoad, bool useSettingAttribute = true);
        /// <summary>
        /// Saves the properties of <paramref name="toSave"/> to <see cref="DataContainer"/>.
        /// </summary>
        /// <typeparam name="T">Generic for the class</typeparam>
        /// <param name="toSave">Instance to save</param>
        /// <param name="useSettingAttribute">
        /// <see langword="true"/> to save only properties with a <see cref="SettingAttribute"/>,
        /// <see langword="false"/> to save all properties of <paramref name="toLoad"/>
        /// </param>
        void Save<T>(T toSave, bool useSettingAttribute = true);
        /// <summary>
        /// Gets a setting from <see cref="DataContainer"/> using a specific key 
        /// (created by formatting <paramref name="globalKey"/> and <paramref name="settingKey"/> with <see cref="SettingFormat"/>)
        /// </summary>
        /// <typeparam name="T">The type of the setting.</typeparam>
        /// <param name="globalKey"></param>
        /// <param name="settingKey"></param>
        /// <returns></returns>
        T GetSetting<T>(string globalKey, string settingKey);
        /// <summary>
        /// <para>
        /// Saves a setting to the provided <see cref="DataContainer"/>, formatting <paramref name="callerName"/> and 
        /// <paramref name="name"/> with <see cref="SettingFormat"/>.
        /// </para>
        /// <para>
        /// If the setting does not exists an entry will be created, if it does the value will be updated.
        /// </para>
        /// </summary>
        /// <param name="callerName">Name of the caller (should be unique)</param>
        /// <param name="name">Name of the setting</param>
        /// <param name="value">Value to save</param>
        void SaveSetting(string callerName, string name, object value);
        /// <summary>
        /// Gets a setting from <see cref="DataContainer"/> using a specific key 
        /// (created by formatting <paramref name="globalKey"/> and <paramref name="settingKey"/> with <see cref="SettingFormat"/>)
        /// <para>
        /// The retrieved setting will not be converted, thus will not throw any conversion exceptions.
        /// </para>
        /// </summary>
        /// <param name="globalKey"></param>
        /// <param name="settingKey"></param>
        /// <returns>The setting value if found (usually a string) or null if the setting was not found.</returns>
        object TryGetSetting(string globalKey, string settingKey);
        /// <summary>
        /// Same as <see cref="GetSetting{T}(string, string)"/>, but with no generics and no exceptions.
        /// </summary>
        /// <param name="settingType">Type to convert the setting to.</param>
        /// <param name="callerName"></param>
        /// <param name="name"></param>
        /// <param name="value">If the setting is found, <paramref name="value"/> will be valued to the setting's value.</param>
        /// <returns>The setting value if found (usually a string) or null if the setting was not found.</returns>
        bool TryGetSetting(Type settingType, string callerName, string name, out object value);
        void RemoveSetting(string globalKey, string settingKey);
    }
}