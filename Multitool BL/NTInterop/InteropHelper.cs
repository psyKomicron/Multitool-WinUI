using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Multitool.NTInterop
{
    internal static class InteropHelper
    {
        public static Win32Exception GetLastError(string message, uint funcRetCode)
        {
            return new(Marshal.GetLastWin32Error(), message + ". (return code " + funcRetCode + ")");
        }

        public static Win32Exception GetLastError(string message)
        {
            return new(Marshal.GetLastWin32Error(), message);
        }
    }
}
