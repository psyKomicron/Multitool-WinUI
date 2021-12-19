using Microsoft.UI.Xaml.Controls;

using Multitool.ComponentModel;
using Multitool.DAL;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultitoolWinUI.Pages
{
    public static class PageExtensions
    {
        public static TSettings LoadSettings<TSettings>(this Page source)
        {
            TSettings ret = default;
            if (ret == null) // is it ever not null ?
            {
                ConstructorInfo settingCtor = typeof(TSettings).GetConstructor(Array.Empty<Type>());
                if (settingCtor != null)
                {
                    ret = (TSettings)Convert.ChangeType(settingCtor.Invoke(Array.Empty<object>()), typeof(TSettings));
                }
                else
                {
                    throw new ArgumentException(typeof(TSettings).Name + " does not have a parameter-less constructor.");
                }
            }

            ISettings settings = App.Settings;
            PropertyInfo[] propertyInfos = typeof(TSettings).GetProperties();

            for (int i = 0; i < propertyInfos.Length; i++)
            {
                if (settings.TryGetSetting(propertyInfos[i].PropertyType, source.GetType().Name, propertyInfos[i].Name, out object value))
                {
                    try
                    {
                        propertyInfos[i].SetValue(ret, value);
                    }
                    catch (ArgumentException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
                else
                {
                    // get default value throught attribute or default ctor
                    IEnumerable<Attribute> attributes = propertyInfos[i].GetCustomAttributes();

                    foreach (Attribute attribute in attributes)
                    {
                        if (attribute.GetType() == typeof(DefaultValueAttribute))
                        {
                            object defaultValue = ((DefaultValueAttribute)attribute).DefaultValue;
                            propertyInfos[i].SetValue(ret, defaultValue);
                            break;
                        }
                    }
                }
            }

            return ret;
        }
    }
}
