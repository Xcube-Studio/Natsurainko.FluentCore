using System;
using System.Runtime.InteropServices;

namespace Natsurainko.FluentCore.Extension.Windows;

public class DllImports
{
    [DllImport("user32.dll")]
    public static extern bool SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateRectRgn(int Left, int Top, int RectRightBottom_X, int RectRightBottom_Y);

    [DllImport("gdi32.dll")]
    public static extern int CombineRgn(IntPtr hrgnDst, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int iMode);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr objectHandle);


    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mEMORY_INFO);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_INFO
    {
        public uint dwLength; //当前结构体大小
        public uint dwMemoryLoad; //当前内存使用率
        public ulong ullTotalPhys; //总计物理内存大小
        public ulong ullAvailPhys; //可用物理内存大小
        public ulong ullTotalPageFile; //总计交换文件大小
        public ulong ullAvailPageFile; //总计交换文件大小
        public ulong ullTotalVirtual; //总计虚拟内存大小
        public ulong ullAvailVirtual; //可用虚拟内存大小
        public ulong ullAvailExtendedVirtual; //保留 这个值始终为0

        public static MEMORY_INFO GetMemoryStatus()
        {
            var mEMORY_INFO = new MEMORY_INFO();
            mEMORY_INFO.dwLength = (uint)Marshal.SizeOf(mEMORY_INFO);
            GlobalMemoryStatusEx(ref mEMORY_INFO);
            return mEMORY_INFO;
        }
    }
}
