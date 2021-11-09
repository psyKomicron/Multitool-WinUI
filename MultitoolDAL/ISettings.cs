using System.Collections.Generic;

using Windows.Storage;

namespace Multitool.DAL
{
    public interface ISettings
    {
        /// <summary>
        /// The <see cref="ApplicationDataContainer"/> associated with this instance.
        /// </summary>
        ApplicationDataContainer DataContainer { get; }
        /// <summary>
        /// How the setting key will be created.
        /// </summary>
        string SettingFormat { get; set; }

        /// <summary>
        /// Retreives a setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callerName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetSetting<T>(string callerName, string name);
        /// <summary>
        /// <para>
        ///     Saves a setting to the provided <see cref="DataContainer"/>, formatting <paramref name="callerName"/>
        ///     and <paramref name="name"/> with <see cref="SettingFormat"/>.
        /// </para>
        /// <para>
        ///     If the setting does not exists an entry will be created, if it does the value will be updated.
        /// </para>
        /// </summary>
        /// <param name="callerName">Name of the caller (should be unique)</param>
        /// <param name="name">Name of the setting</param>
        /// <param name="value">Value to save</param>
        void SaveSetting(string callerName, string name, object value);
        /// <summary>
        /// Same as <see cref="GetSetting{T}(string, string)"/> but instead does not throw exceptions.
        /// </summary>
        /// <param name="callerName"></param>
        /// <param name="name"></param>
        /// <returns>
        /// <see langword="null"/> if no setting was associated to the key (created by formatting <paramref name="callerName"/> 
        /// and <paramref name="name"/> with <see cref="SettingFormat"/>) or the setting.
        /// </returns>
#nullable enable
        object? TryGetSetting(string callerName, string name);
#nullable disable
        /// <summary>
        /// Returns all settings saved.
        /// </summary>
        /// <returns>All settings maped by their name</returns>
        Dictionary<string, object> GetAllSettings();
        /// <summary>
        /// Saves a setting using <paramref name="setting"/> as the setting key and <paramref name="converter"/>
        /// as a converter to a saveable type (primitive type and primitive arrays).
        /// </summary>
        /// <typeparam name="TIn"><see langword="in"/> type for the converter</typeparam>
        /// <typeparam name="TOut"><see langword="out"/> type for the converter</typeparam>
        /// <param name="setting">Key to retreive the setting</param>
        /// <param name="converter">Converter to convert <paramref name="value"/></param>
        /// <param name="value">Value to save</param>
        void SaveSetting<TIn, TOut>(string setting, ISettingConverter<TIn, TOut> converter, TIn value);
    }
}