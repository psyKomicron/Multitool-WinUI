using System.Xml;

namespace Multitool.DAL.Settings
{
    public interface ISettingConverter
    {
        XmlNode Convert(object toConvert);
        object Restore(XmlNode toRestore);
    }
}
