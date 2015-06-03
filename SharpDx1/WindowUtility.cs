using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpDx1
{
    class WindowUtility
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public static Rect GetWindowRect(string processName)
        {
            var p = Process.GetProcesses();
            Process[] processes = Process.GetProcessesByName(processName);
            Process lol = processes[0];
            IntPtr ptr = lol.MainWindowHandle;
            Rect rect = new Rect();
            GetWindowRect(ptr, ref rect);

            return rect;
        }
    }
}