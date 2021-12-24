using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.DAL.Settings
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SettingAttribute : Attribute
    {
        /// <summary>
        /// Marks the property as a setting, so that a setting loader will load the corresponding setting. If no corresponding setting is found, the property will be instanciated with an instance of <paramref name="propertyType"/>.
        /// </summary>
        /// <param name="propertyType"></param>
        public SettingAttribute(Type converterType, params object[] parameters)
        {
            if (converterType != null)
            {
                try
                {
                    ConstructorInfo ctorInfo = converterType.GetConstructor(Array.Empty<Type>());
                    if (ctorInfo != null)
                    {
                        Converter = (SettingConverter)Convert.ChangeType(ctorInfo.Invoke(Array.Empty<object>()), converterType);
                    }
                }
                catch { }
            }
            DefaultValue = parameters;
            WantsDefaultValue = true;
        }

        public SettingAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
        }

        public SettingAttribute() { }

        internal object DefaultValue { get; }

        internal bool HasDefaultValue { get; }

        internal bool WantsDefaultValue { get; }

        internal SettingConverter Converter { get; }
    }
}
