using System.Runtime.InteropServices;

namespace SampleClient
{
    /// <summary>
    /// MonitorInfo class provides methods to retrieve information about the primary monitor
    /// </summary>
    internal static class MonitorInfo
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int HORZRES = 8;
        private const int VERTRES = 10;
        private const int HORZSIZE = 4;
        private const int VERTSIZE = 6;

        /// <summary>
        /// Get the resolution of the primary screen in pixels
        /// </summary>
        /// <returns></returns>
        public static (int width, int height) GetPrimaryScreenResolution()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int width = GetDeviceCaps(hdc, HORZRES);
            int height = GetDeviceCaps(hdc, VERTRES);
            ReleaseDC(IntPtr.Zero, hdc);
            return (width, height);
        }

        /// <summary>
        /// Get the size of the primary screen in millimeters
        /// </summary>
        /// <returns></returns>
        public static (double width, double height) GetPrimaryScreenSizeInMillimeters()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            double width = GetDeviceCaps(hdc, HORZSIZE);
            double height = GetDeviceCaps(hdc, VERTSIZE);
            ReleaseDC(IntPtr.Zero, hdc);
            return (width, height);
        }
    }
}
