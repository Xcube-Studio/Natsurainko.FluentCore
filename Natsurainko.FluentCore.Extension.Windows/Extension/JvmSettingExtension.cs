using Natsurainko.FluentCore.Model.Launch;
using System;
using static Natsurainko.FluentCore.Extension.Windows.DllImports;

namespace Natsurainko.FluentCore.Extension.Windows.Extension;

public static class JvmSettingExtension
{
    public static void AutoSetMemory(this JvmSetting jvmSetting, int minimum = 512)
    {
        var memoryInfo = MEMORY_INFO.GetMemoryStatus();
        var willUsed = (memoryInfo.ullAvailPhys / 1024 / 1024) * 0.75;

        jvmSetting.MaxMemory = willUsed < minimum 
            ? minimum 
            : Convert.ToInt32(willUsed);
        jvmSetting.MinMemory = minimum;
    }
}
