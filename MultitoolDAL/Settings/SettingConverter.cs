namespace Multitool.DAL.Settings
{
    public abstract class SettingConverter
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SettingConverter() { }

        public bool IsSingleLineValue { get; protected set; }

        public abstract object Convert(object toConvert);
        public abstract object Restore(object toRestore);
    }
}
