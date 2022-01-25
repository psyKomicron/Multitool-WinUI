using System;
using System.Reflection;

namespace Multitool.DAL.Settings
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SettingAttribute : Attribute
    {
        /// <summary>
        /// <para>
        /// Sets the property to be saved by a <see cref="SettingsManager"/>.
        /// </para>
        /// <para>
        /// The class will create an instance of <see cref="SettingConverter"/> (<paramref name="converterType"/>) to convert saved value back and forth.
        /// </para>
        /// </summary>
        /// <param name="converterType"></param>
        public SettingAttribute(Type converterType, bool defaultInstanciate = true, string settingName = null)
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

            SettingName = string.IsNullOrEmpty(settingName) ? null : settingName;
            DefaultInstanciate = defaultInstanciate;
        }

        /// <summary>
        /// Creates a <see cref="SettingAttribute"/>, the property will be instanciated with <paramref name="defaultValue"/>
        /// if the setting does not exists
        /// </summary>
        /// <param name="defaultValue"></param>
        public SettingAttribute(object defaultValue, string settingName = null)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
            SettingName = string.IsNullOrEmpty(settingName) ? null : settingName;
        }

        /// <summary>
        /// Default parameter-less constructor.
        /// </summary>
        public SettingAttribute()
        {
            DefaultInstanciate = true;
        }

        /// <summary>
        /// Setting default value.
        /// </summary>
        /// <remarks>
        /// Do not check if the property is <see langword="null"/>, but use the <see cref="HasDefaultValue"/> property to check if you can use
        /// the property
        /// </remarks>
        public object DefaultValue { get; init; }

        public bool HasDefaultValue { get; init; }

        public bool DefaultInstanciate { get; init; }

        public SettingConverter Converter { get; init; }

        public string SettingName { get; init; }
    }
}
