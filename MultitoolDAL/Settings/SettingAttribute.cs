using Multitool.Data.Settings.Converters;

using System;
using System.Diagnostics;
using System.Reflection;

namespace Multitool.Data.Settings
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SettingAttribute : Attribute
    {
        private object defaultValue;

        #region constructors
        /// <summary>
        /// <para>
        /// Sets the property to be saved by a <see cref="IUserSettingsManager"/>.
        /// </para>
        /// <para>
        /// The class will create an instance of <see cref="IUserSettingsManager"/> (<paramref name="converterType"/>) to convert saved value back and forth.
        /// </para>
        /// </summary>
        /// <param name="converterType"></param>
        public SettingAttribute(Type converterType)
        {
            if (converterType != null)
            {
                try
                {
                    ConstructorInfo ctorInfo = converterType.GetConstructor(Array.Empty<Type>());
                    if (ctorInfo != null)
                    {
                        Converter = (ISettingConverter)Convert.ChangeType(ctorInfo.Invoke(Array.Empty<object>()), converterType);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to create custom setting converter: {ex}");
                }
            }
            DefaultInstanciate = true;
        }

        /// <summary>
        /// Creates a setting attribute with the provided setting key.
        /// </summary>
        /// <param name="memberType">Type to associate the setting with.</param>
        /// <param name="settingName">Name of the setting (for this property).</param>
        public SettingAttribute(Type memberType, string settingName)
        {
            SettingKey = memberType.FullName;
            SettingName = settingName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberType">Type to associate the setting with.</param>
        /// <param name="settingName">Name of the setting (for this property).</param>
        /// <param name="converterType"><see cref="ISettingConverter"/> to convert the setting with.</param>
        public SettingAttribute(Type memberType, string settingName, Type converterType) : this(converterType)
        {
            SettingKey = memberType.FullName;
            SettingName = settingName;
        }

        /// <summary>
        /// Creates a <see cref="SettingAttribute"/>, the property will be instanciated with <paramref name="defaultValue"/>
        /// if the setting does not exists
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <param name="settingName"></param>
        public SettingAttribute(object defaultValue, string settingName = null)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
            SettingName = string.IsNullOrEmpty(settingName) ? null : settingName;
        }

        /// <summary>
        /// Creates a <see cref="SettingAttribute"/> with an instance of the provided <paramref name="converterType"/> as converter
        /// and <paramref name="parameters"/> as default value if the setting does not exists.
        /// </summary>
        /// <param name="converterType"></param>
        /// <param name="parameters"></param>
        public SettingAttribute(Type converterType, params object[] parameters) : this(converterType)
        {
            DefaultValue = parameters;
            HasDefaultValue = true;
        }

        /// <summary>
        /// Default parameter-less constructor.
        /// </summary>
        public SettingAttribute()
        {
            DefaultInstanciate = true;
        }
        #endregion

        #region properties
        /// <summary>
        /// The 2 way converter (convert, restore) to convert this setting.
        /// </summary>
        /// <remarks>
        /// Do not use this property to associate a converter with this instance of <see cref="SettingAttribute"/>, use
        /// the following constructors
        /// <list>
        ///     <item>
        ///         <see cref="SettingAttribute(Type)"/>
        ///     </item>
        ///     <item>
        ///         <see cref="SettingAttribute(Type, string)"/>
        ///     </item>
        ///     <item>
        ///         <see cref="SettingAttribute(Type, object[])"/>
        ///     </item>
        /// </list>
        /// </remarks>
        public ISettingConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets the default value for this setting.
        /// Setting the property will automatically set <see cref="HasDefaultValue"/> to <see langword="true"/>.
        /// <para>Defaults to <see langword="null"/></para>
        /// </summary>
        /// <remarks>
        /// Do not check if the property is <see langword="null"/>, but use the <see cref="HasDefaultValue"/> property to check if you can use
        /// the property
        /// </remarks>
        public object DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
                HasDefaultValue = true;
            }
        }

        /// <summary>
        /// Tells if the attribute has a default value that should be used if the setting is not found.
        /// <para>Defaults to <see langword="false"/></para>
        /// </summary>
        public bool HasDefaultValue { get; set; }

        /// <summary>
        /// Tells to instanciate this property with the property type default contructor if the setting is not found.
        /// <para>Defaults to <see langword="true"/> if not default value is given.</para>
        /// </summary>
        public bool DefaultInstanciate { get; set; }

        /// <summary>
        /// Gets or sets the key of the setting. A setting is created with the combination of <see cref="SettingKey"/> and <see cref="SettingName"/>.
        /// <para>Defaults <see cref="string.Empty"/></para>
        /// </summary>
        /// <remarks>
        /// Setting this property is not needed for setting creation since the setting manager will defaults the setting key to the property's
        /// declaring type.
        /// </remarks>
        public string SettingKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the setting. A setting is created with the combination of <see cref="SettingKey"/> and <see cref="SettingName"/>.
        /// <para>Defaults <see cref="string.Empty"/></para>
        /// </summary>
        /// <remarks>
        /// Setting this property is not needed for setting creation since the setting manager will defaults the setting key to the property's name.
        /// </remarks>
        public string SettingName { get; set; } 
        #endregion
    }
}
