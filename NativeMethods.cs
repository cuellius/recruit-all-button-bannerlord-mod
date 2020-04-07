using System;
using System.Runtime.InteropServices;

namespace RecruitAllButton
{
    public class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
