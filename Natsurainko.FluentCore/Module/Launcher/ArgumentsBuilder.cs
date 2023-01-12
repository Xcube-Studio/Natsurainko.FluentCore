using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.Toolkits.Text;
using Natsurainko.Toolkits.Values;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Natsurainko.FluentCore.Module.Launcher;

public class ArgumentsBuilder : IArgumentsBuilder
{
    public IGameCore GameCore { get; private set; }

    public LaunchSetting LaunchSetting { get; private set; }

    public ArgumentsBuilder(IGameCore gameCore, LaunchSetting launchSetting)
    {
        GameCore = gameCore;
        LaunchSetting = launchSetting;
    }

    public IEnumerable<string> Build()
    {
        foreach (var item in GetFrontArguments())
            yield return item;

        yield return GameCore.MainClass;

        foreach (var item in GetBehindArguments())
            yield return item;
    }

    public IEnumerable<string> GetFrontArguments()
    {
        var keyValuePairs = new Dictionary<string, string>()
        {
            { "${launcher_name}", "Natsurainko.FluentCore" },
            { "${launcher_version}", "3" },
            { "${classpath_separator}", Path.PathSeparator.ToString() },
            { "${classpath}", GetClasspath().ToPath() },
            { "${client}", GameCore.ClientFile.FileInfo.FullName.ToPath() },
            { "${min_memory}", LaunchSetting.JvmSetting.MinMemory.ToString() },
            { "${max_memory}", LaunchSetting.JvmSetting.MaxMemory.ToString() },
            { "${library_directory}", Path.Combine(GameCore.Root.FullName, "libraries").ToPath() },
            {
                "${version_name}",
                string.IsNullOrEmpty(GameCore.InheritsFrom)
                ? GameCore.Id
                : GameCore.InheritsFrom
            },
            {
                "${natives_directory}",
                LaunchSetting.NativesFolder != null && LaunchSetting.NativesFolder.Exists
                ? LaunchSetting.NativesFolder.FullName.ToString()
                : Path.Combine(GameCore.Root.FullName, "versions", GameCore.Id, "natives").ToPath()
            }
        };

        if (!Directory.Exists(keyValuePairs["${natives_directory}"]))
            Directory.CreateDirectory(keyValuePairs["${natives_directory}"].Trim('\"'));

        var args = new List<string>()
        {
            "-Xms${min_memory}M",
            "-Xmx${max_memory}M",
            "-Dminecraft.client.jar=${client}",
        };

        foreach (var item in GetEnvironmentJVMArguments())
            args.Add(item);

        LaunchSetting.JvmSetting.GCArguments?.ForEach(item => args.Add(item));
        LaunchSetting.JvmSetting.AdvancedArguments?.ForEach(item => args.Add(item));

        args.Add("-Dlog4j2.formatMsgNoLookups=true");

        foreach (var item in GameCore.FrontArguments)
            args.Add(item);

        foreach (var item in args)
            yield return item.Replace(keyValuePairs);
    }

    public IEnumerable<string> GetBehindArguments()
    {
        var keyValuePairs = new Dictionary<string, string>()
        {
            { "${auth_player_name}" , LaunchSetting.Account.Name },
            { "${version_name}" , GameCore.Id },
            { "${assets_root}" , Path.Combine(GameCore.Root.FullName, "assets").ToPath() },
            { "${assets_index_name}" , Path.GetFileNameWithoutExtension(GameCore.AssetIndexFile.FileInfo.FullName) },
            { "${auth_uuid}" , LaunchSetting.Account.Uuid.ToString("N") },
            { "${auth_access_token}" , LaunchSetting.Account.AccessToken },
            { "${user_type}" , "Mojang" },
            { "${version_type}" , GameCore.Type },
            { "${user_properties}" , "{}" },
            { "${game_assets}" , Path.Combine(GameCore.Root.FullName, "assets").ToPath() },
            { "${auth_session}" , LaunchSetting.Account.AccessToken },
            {
                "${game_directory}" ,
                    (LaunchSetting.EnableIndependencyCore && (bool)LaunchSetting.WorkingFolder?.Exists
                        ? LaunchSetting.WorkingFolder.FullName
                        : GameCore.Root.FullName).ToPath()
            },
        };

        var args = GameCore.BehindArguments.ToList();

        if (LaunchSetting.GameWindowSetting != null)
        {
            args.Add($"--width {LaunchSetting.GameWindowSetting.Width}");
            args.Add($"--height {LaunchSetting.GameWindowSetting.Height}");

            if (LaunchSetting.GameWindowSetting.IsFullscreen)
                args.Add("--fullscreen");
        }

        if (LaunchSetting.IsDemoUser)
            args = args.Append("--demo").ToList();

        if (LaunchSetting.ServerSetting != null && !string.IsNullOrEmpty(LaunchSetting.ServerSetting.IPAddress) && LaunchSetting.ServerSetting.Port != 0)
        {
            args.Add($"--server {LaunchSetting.ServerSetting.IPAddress}");
            args.Add($"--port {LaunchSetting.ServerSetting.Port}");
        }

        foreach (var item in args)
            yield return item.Replace(keyValuePairs);
    }

    public string GetClasspath()
    {
        var loads = new List<IResource>();

        GameCore.LibraryResources.ForEach(x =>
        {
            if (x.IsEnable && !x.IsNatives)
                loads.Add(x);
        });

        loads.Add(GameCore.ClientFile);

        return string.Join(Path.PathSeparator.ToString(), loads.Select(x => x.ToFileInfo().FullName));
    }

    public static IEnumerable<string> GetEnvironmentJVMArguments()
    {
        switch (EnvironmentInfo.GetPlatformName())
        {
            case "windows":
                yield return "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump";
                if (Environment.OSVersion.Version.Major == 10)
                {
                    yield return "-Dos.name=\"Windows 10\"";
                    yield return "-Dos.version=10.0";
                }
                break;
            case "osx":
                yield return "-XstartOnFirstThread";
                break;
        }

        if (EnvironmentInfo.Arch == "32")
            yield return "-Xss1M";
    }
}
