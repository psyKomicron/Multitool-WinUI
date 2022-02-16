using System.Collections.Generic;

using Windows.Foundation;

namespace Multitool.Data.Settings
{
    /// <summary>
    /// Defines behavior for classes handling app settings.
    /// </summary>
    public interface ISettingsManager
    {
        event TypedEventHandler<ISettingsManager, string> SettingsChanged;

        string SettingFilePath { get; }

        void Commit();
        void EditSetting(string globalKey, string settingKey, object value);
        T GetSetting<T>(string globalKey, string settingKey);
        void Load<T>(T toLoad, bool useSettingAttribute = true);
        List<string> ListSettingsKeys();
        List<string> ListSettingsKeys(string globalKey);
        void RemoveSetting(string globalKey, string settingKey);
        void Reset();
        void Save<T>(T toSave, bool useSettingAttribute = true);
        void SaveSetting(string globalKey, string name, object value);
        object TryGetSetting(string globalKey, string settingKey);
        bool TryGetSetting<T>(string globalKey, string name, out T value);
    }
}