using System;

namespace Multitool.Data.Settings
{
    /// <summary>
    /// Thrown when the setting type does not equals the requested type.
    /// </summary>
    public class SettingTypeMismatch : Exception
    {
        private readonly string toString;

        public SettingTypeMismatch(string objectType, string savedType, string message) : base(message)
        {
            toString = $"Parameter/property type ({objectType}) does not match the type saved ({savedType}).";
        }

        public override string ToString()
        {
            if (Message != null)
            {
                return $"{Message}\n{toString}";
            }
            else
            {
                return toString;
            }
        }
    }
}
