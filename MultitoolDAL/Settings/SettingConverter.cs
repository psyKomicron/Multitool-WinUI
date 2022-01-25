using System.Xml;

namespace Multitool.DAL.Settings
{
    public abstract class SettingConverter
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SettingConverter() { }

        public abstract XmlNode Convert(object toConvert);
        public abstract object Restore(XmlNode toRestore);
    }
}
