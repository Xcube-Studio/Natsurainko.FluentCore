using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Service;
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

    public List<string> JvmArguments { get; set; } = DefaultSettings.DefaultJvmArguments;

    public JvmSetting() { }

    public JvmSetting(string file) => Javaw = new FileInfo(file);

    public JvmSetting(FileInfo fileInfo) => Javaw = fileInfo;
}

public class GameWindowSetting
{
    public int Width { get; set; } = 854;

    public int Height { get; set; } = 480;

    public bool IsFullscreen { get; set; } = false;

    public string WindowTitle { get; set; }
}

public class ServerSetting
{
    public ServerSetting() { }

    public ServerSetting(string iPAddress)
    {
        var address = iPAddress.Split(':');
        IPAddress = address[0];
        Port = address.Length == 2 ? int.Parse(address[1]) : 25565;
    }

    public ServerSetting(string iPAddress, int port)
    {
        IPAddress = iPAddress;
        Port = port;
    }

    public string IPAddress { get; set; }

    public int Port { get; set; }

    public override string ToString()
    {
        if (Port == 25565)
            return IPAddress;

        else return IPAddress + ":" + Port.ToString();
    }
}