using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Windows.ApplicationModel;

namespace MultitoolWinUI.Helpers
{
    internal static class Tool
    {
        private static readonly Regex absoluteUriRegex = new(@"(https?: \/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public const string BuildType =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        public static string GetPackageVersion()
        {
            System.Text.StringBuilder builder = new();
            builder.Append(Package.Current.Id.Version.Major)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Minor)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Build)
                   .Append('.')
                   .Append(Package.Current.Id.Version.Revision);
            return builder.ToString();
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

        public static bool IsRelativeUrl(string s) => absoluteUriRegex.IsMatch(s);

        private static T GetValueFromDictionary<T, K>(IDictionary<K, object> dic, K key)
        {
            if (dic.ContainsKey(key))
            {
                object o = dic[key];
                if (o is T)
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
        TERA = 0x10000000000,
        GIGA = 0x40000000,
        MEGA = 0x100000,
        KILO = 0x400
    }
}
