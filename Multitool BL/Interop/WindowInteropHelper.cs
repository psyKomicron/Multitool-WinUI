
using Multitool.Interop;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Multitool.BL.Interop
{
    public class WindowInteropHelper
    {
        /// <summary>
        /// Sets a window position.
        /// </summary>
        /// <param name="windowHandle">HWND for the window</param>
        /// <param name="newPosition">the new position of the window</param>
        public static void SetWindow(object window, Windows.Foundation.Size size, Windows.Graphics.PointInt32 position = default)
        {
            HWND hwnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(window);
            // Win32 uses pixels and WinUI 3 uses effective pixels, so you should apply the DPI scale factor
            uint dpi = PInvoke.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            int width = (int)size.Width;
            int height = (int)size.Height;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            SET_WINDOW_POS_FLAGS flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER;
            if (position == default)
            {
                flags |= SET_WINDOW_POS_FLAGS.SWP_NOMOVE;
            }
            else
            {
                flags |= SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS;
            }

            if (!PInvoke.SetWindowPos(hwnd, default, position.X, position.Y, width, height, flags))
            {
                throw InteropHelper.GetLastError("Failed to set window position/size.");
            }
        }
    }
}
