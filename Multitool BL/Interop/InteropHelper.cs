using System.ComponentModel;
using System.Runtime.InteropServices;

using static Multitool.Interop.Codes.SystemCodes;

namespace Multitool.Interop
{
    internal static class InteropHelper
    {
        public static Win32Exception GetLastError(string message, uint funcRetCode)
        {
            return new(Marshal.GetLastWin32Error(), $"{message}. (HRESULT: {funcRetCode})");
        }

        public static Win32Exception GetLastError(string message)
        {
            return new(Marshal.GetLastWin32Error(), message);
        }

        public static Win32Exception GetLastError(uint funcRetCode)
        {
            string message = funcRetCode switch
            {
                ERROR_ACCESS_DENIED => "Access is denied.",
                ERROR_INVALID_ACCESS => "The access code is invalid.",
                ERROR_SHUTDOWN_USERS_LOGGED_ON => "Shutdown is not possible, other users are logged on",
                _ => "Function returned non-zero code.",
            };
            return GetLastError(message, funcRetCode);
        }
    }
}
