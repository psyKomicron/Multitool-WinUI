using Windows.Storage;

namespace Multitool.DAL
{
    public interface ISettings
    {
        /// <summary>
        /// The <see cref="ApplicationDataContainer"/> associated with this instance
        /// </summary>
        ApplicationDataContainer DataContainer { get; }
        /// <summary>
        /// How the setting key will be created
        /// </summary>
        string SettingFormat { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callerName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetSetting<T>(string callerName, string name);
        /// <summary>
        /// <para>
        /// Saves a setting to the provided <see cref="DataContainer"/>, formatting <paramref name="callerName"/> and <paramref name="name"/> with <see cref="SettingFormat"/>.
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
        /// 
        /// </summary>
        /// <param name="callerName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        object TryGetSetting(string callerName, string name);
    }
}