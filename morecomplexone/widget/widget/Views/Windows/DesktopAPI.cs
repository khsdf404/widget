using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace widget
{
    class DesktopAPI
    {
        static IntPtr hDesktop = IntPtr.Zero;

        delegate bool EnumCallback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumCallback lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GWConstants iCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, [Out] StringBuilder buf, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        enum GWConstants : int
        {
            GW_HWNDFIRST,
            GW_HWNDLAST,
            GW_HWNDNEXT,
            GW_HWNDPREV,
            GW_OWNER,
            GW_CHILD,
            GW_MAX
        }

        static string GetClassNameFromHWND(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            int len = GetClassName(hWnd, sb, sb.Capacity);
            if (len > 0)
                return sb.ToString(0, len);

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        static bool EnumWins(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd != IntPtr.Zero)
            {
                IntPtr hDesk = GetWindow(hWnd, GWConstants.GW_CHILD);
                if (hDesk != IntPtr.Zero && GetClassNameFromHWND(hDesk) == "SHELLDLL_DefView")
                {
                    hDesk = GetWindow(hDesk, GWConstants.GW_CHILD);
                    if (hDesk != IntPtr.Zero && GetClassNameFromHWND(hDesk) == "SysListView32")
                    {
                        hDesktop = hDesk;
                        return false;
                    }
                }
            }
            return true;
        }

        public static void WindowOnDesktopShow(IntPtr window)
        {
            if (hDesktop != IntPtr.Zero && !IsWindowVisible(hDesktop))
                SetParent(window, hDesktop);
            else
            {
                EnumWindows(new EnumCallback(EnumWins), (IntPtr)5);
                SetParent(window, hDesktop);
            }
        }
    }
}
