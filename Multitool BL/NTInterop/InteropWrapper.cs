using System;
using System.Drawing;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Multitool.NTInterop
{
    public static class InteropWrapper
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out ulong _lpPoint);

        public static Point GetCursorPosition()
        {
            if (!GetCursorPos(out ulong pos))
            {
                throw InteropHelper.GetLastError("Failed to get cursor position ('GetCursorPos')");
            }
            int x = (int)(pos & 0x00000000FFFFFFFF);
            int y = (int)((pos & 0xFFFFFFFF00000000) >> 32);
            return new Point(x, y);
        }

        public static void GetCursorPosition(out int x, out int y)
        {
            if (!GetCursorPos(out ulong pos))
            {
                throw InteropHelper.GetLastError("Failed to get cursor position ('GetCursorPos')");
            }
            x = (int)(pos & 0x00000000FFFFFFFF);
            y = (int)((pos & 0xFFFFFFFF00000000) >> 32);
        }

        public static void SetWindowsPosition(IntPtr windowHandle, Point newPosition)
        {
            HWND hwnd = Marshal.PtrToStructure<HWND>(windowHandle);

            if (!PInvoke.GetWindowRect(hwnd, out RECT rect))
            {
                throw InteropHelper.GetLastError("'GetWindowRect' failed");
            }

            Size s = new()
            {
                Height = rect.bottom - rect.top,
                Width = rect.right - rect.left
            };

            if (!PInvoke.SetWindowPos(hwnd, new(-1), newPosition.X, newPosition.Y, s.Width, s.Height, SET_WINDOW_POS_FLAGS.SWP_NOMOVE))
            {
                throw InteropHelper.GetLastError("'SetWindowPos' failed");
            }
        }
    }
}
