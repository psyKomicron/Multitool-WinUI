using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Multitool.Data.Win32
{
    public static class RegistryHelper
    {
        public static string[] GetExtensionsForMime(Regex mimeType)
        {
            List<string> exts = new();
            RegistryKey classesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\");
            if (classesKey != null)
            {
                string[] names = classesKey.GetSubKeyNames();
                for (int i = 0; i < names.Length; i++)
                {
                    RegistryKey key = classesKey.OpenSubKey(names[i]);
                    if (key.GetValueNames().Contains("Content Type"))
                    {
                        object contentType = key.GetValue("Content Type");
                        if (mimeType.IsMatch(contentType.ToString()))
                        {
                            exts.Add(names[i]);
                        }
                    }
                }
            }
            return exts.ToArray();
        }
    }
}

