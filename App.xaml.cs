using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RegistryKey_Check
{
    public partial class App : Application
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void UseDarkTitleBar(Window window)
        {
            window.SourceInitialized += (sender, e) =>
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                int dark = 1;
                if (DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
                }
            };
        }
    }
}
