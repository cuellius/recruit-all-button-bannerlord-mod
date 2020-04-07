using System;
using System.Runtime.InteropServices;

namespace RecruitAllButton
{
    public class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        // ReSharper disable InconsistentNaming
        public const uint MB_OK = 0;
        public const uint MB_ICONERROR = 0x10;
        // ReSharper restore InconsistentNaming
    }
}
