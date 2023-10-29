using Nrk.FluentCore.Classes.Datas;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Nrk.FluentCore.Utils;

public static partial class MemoryUtils
{
    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

    [SupportedOSPlatform("windows")]
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
    }

    [Obsolete("在部分 Windows 上有莫名奇妙的问题")]
    [SupportedOSPlatform("windows")]
    public static MemoryMetrics GetWindowsMetricsFromPowershell()
    {
        using var process = Process.Start(new ProcessStartInfo()
        {
            FileName = "powershell",
            Arguments = "-NoLogo -NonInteractive -Command \"Get-CIMInstance Win32_OperatingSystem | Select FreePhysicalMemory,TotalVisibleMemorySize | Format-List\"",
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        });

        process.WaitForExit();

        var lines = process.StandardOutput.ReadToEnd().Trim().Split("\n");
        var freeMemoryParts = lines[0].Split(":", StringSplitOptions.RemoveEmptyEntries);
        var totalMemoryParts = lines[1].Split(":", StringSplitOptions.RemoveEmptyEntries);

        var total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
        var free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);

        return new MemoryMetrics
        {
            Total = total,
            Free = free,
            Used = total - free
        };
    }

    [SupportedOSPlatform("windows")]
    public static MemoryMetrics GetWindowsMetrics()
    {
        var mi = new MEMORY_INFO();
        mi.dwLength = (uint)Marshal.SizeOf(mi);
        GlobalMemoryStatusEx(ref mi);

        var total = Math.Round((double)mi.ullTotalPhys / (1024 * 1024), 0);
        var free = Math.Round((double)mi.ullAvailPhys / (1024 * 1024), 0);

        return new MemoryMetrics
        {
            Total = total,
            Free = free,
            Used = total - free
        };
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    public static MemoryMetrics GetUnixMetrics()
    {
        using var process = Process.Start(new ProcessStartInfo("free -m")
        {
            FileName = "/bin/bash",
            Arguments = "-c \"free -m\"",
            RedirectStandardOutput = true
        });

        process.WaitForExit();

        var lines = process.StandardOutput.ReadToEnd().Split("\n");
        var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

        return new MemoryMetrics
        {
            Total = double.Parse(memory[1]),
            Used = double.Parse(memory[2]),
            Free = double.Parse(memory[3])
        };
    }

    public static (int, int) CalculateJavaMemory(int min = 512)
    {
#pragma warning disable CA1416
        var metrics = EnvironmentUtils.PlatformName.Equals("windows")
            ? GetWindowsMetrics()
            : GetUnixMetrics();
#pragma warning restore CA1416

        var willUsed = metrics.Free * 0.6;
        var max = willUsed < min ? min : Convert.ToInt32(willUsed);

        return (max, min);
    }
}
