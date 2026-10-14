using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RegistryKey_Check
{
    internal static class WindowTheme
    {
        private const int DarkModeAttribute = 20;

        private const int CaptionColorAttribute = 35;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void UseDarkTitleBar(Window window)
        {
            window.SourceInitialized += (sender, e) =>
            {
                try
                {
                    IntPtr handle = new WindowInteropHelper(window).Handle;
                    int dark = 1;
                    DwmSetWindowAttribute(handle, DarkModeAttribute, ref dark, sizeof(int));
                    int caption = 0x002A170F;
                    DwmSetWindowAttribute(handle, CaptionColorAttribute, ref caption, sizeof(int));
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
            };
        }
    }
}
