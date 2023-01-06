using Natsurainko.FluentCore.Interface;
using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Model.Launch;

public class LaunchSetting
{
    public DirectoryInfo NativesFolder { get; set; }

    public DirectoryInfo WorkingFolder { get; set; }

    public IAccount Account { get; set; }

    public JvmSetting JvmSetting { get; set; }

    public GameWindowSetting GameWindowSetting { get; set; } = new GameWindowSetting();

    public ServerSetting ServerSetting { get; set; }

    public bool IsDemoUser { get; set; } = false;

    public bool EnableIndependencyCore { get; set; } = false;

    public LaunchSetting() { }

    public LaunchSetting(JvmSetting jvmSetting)
    {
        JvmSetting = jvmSetting;
    }
}

public class JvmSetting
{
    public FileInfo Javaw { get; set; }

    public int MaxMemory { get; set; } = 2048;

    public int MinMemory { get; set; } = 512;

    public IEnumerable<string> AdvancedArguments { get; set; }

    public IEnumerable<string> GCArguments { get; set; }

    public JvmSetting() { }

    public JvmSetting(string file) => Javaw = new FileInfo(file);

    public JvmSetting(FileInfo fileInfo) => Javaw = fileInfo;
}

public class GameWindowSetting
{
    public int Width { get; set; } = 854;

    public int Height { get; set; } = 480;

    public bool IsFullscreen { get; set; } = false;
}

public class ServerSetting
{
    public string IPAddress { get; set; }

    public int Port { get; set; }
}