using System;

namespace Multitool.Data
{
    public class SettingNotFoundException : Exception
    {
        public SettingNotFoundException(string settingName, string message = "Setting not found") : base(message + ". Setting name: " + settingName)
        {
            SettingName = settingName;
        }

        public string SettingName { get; }
    }
}
