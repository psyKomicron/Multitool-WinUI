using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Windows.Storage;

namespace MultitoolWinUI.Helpers
{
    internal static class Tool
    {
        public static T GetSetting<T>(string key)
        {
            return GetValueFromDictionary<T, string>(ApplicationData.Current.LocalSettings.Values, key);
        }

        public static T GetAppRessource<T>(string name)
        {
            return GetValueFromDictionary<T, object>(Application.Current.Resources, name);
        }

        public static string FormatSize(long size)
        {
            if (size >= (long)Sizes.TERA)
            {
                return Math.Round(size / (double)Sizes.TERA, 3) + " Tb";
            }
            if (size >= (long)Sizes.GIGA)
            {
                return Math.Round(size / (double)Sizes.GIGA, 3) + " Gb";
            }
            if (size >= (long)Sizes.MEGA)
            {
                return Math.Round(size / (double)Sizes.MEGA, 3) + " Mb";
            }
            if (size >= (long)Sizes.KILO)
            {
                return Math.Round(size / (double)Sizes.KILO, 3) + " Kb";
            }
            return size + " b";
        }

        public static void FormatSize(long size, out double formatted, out string ext)
        {
            if (size >= (long)Sizes.TERA)
            {
                formatted = Math.Round(size / (double)Sizes.TERA, 3);
                ext = " Tb";
            }
            else if (size >= (long)Sizes.GIGA)
            {
                formatted = Math.Round(size / (double)Sizes.GIGA, 3);
                ext = " Gb";
            }
            else if (size >= (long)Sizes.MEGA)
            {
                formatted = Math.Round(size / (double)Sizes.MEGA, 3);
                ext = " Mb";
            }
            else if (size >= (long)Sizes.KILO)
            {
                formatted = Math.Round(size / (double)Sizes.KILO, 3);
                ext = " Kb";
            }
            else
            {
                formatted = size;
                ext = " b";
            }
        }

        public static void TryEnqueue(DispatcherQueue queue, PropertyChangedEventHandler callback, object sender, [CallerMemberName] string name = null)
        {
            if (callback != null)
            {
                _ = queue.TryEnqueue(() => callback?.Invoke(sender, new PropertyChangedEventArgs(name)));
            }
            else
            {
                Trace.WriteLine("Not enqueueing callback to dispacher (callback null)");
            }
        }

        private static T GetValueFromDictionary<T, K>(IDictionary<K, object> dic, K key)
        {
            if (dic.ContainsKey(key))
            {
                object o = dic[key];
                if (o.GetType() == typeof(T))
                {
                    return (T)dic[key];
                }
                else
                {
                    throw new InvalidCastException(o.GetType().ToString() + " cannot be directly casted to " + typeof(T).ToString());
                }
            }
            else
            {
                throw new ArgumentException("Application resources doesn't contains '" + key + "'");
            }
        }
    }

    internal enum Sizes : long
    {
        TERA = 1000000000000,
        GIGA = 1000000000,
        MEGA = 1000000,
        KILO = 1000
    }
}
