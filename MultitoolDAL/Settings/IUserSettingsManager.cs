namespace Multitool.Data.Settings
{
    /// <summary>
    /// Defines behavior for classes handling app settings.
    /// </summary>
    public interface IUserSettingsManager : ISettingsManager
    {
        string SettingFilePath { get; }

        void Commit();
        void Load<T>(T toLoad, bool useSettingAttribute = true);
        void Save<T>(T toSave, bool useSettingAttribute = true);
    }
}