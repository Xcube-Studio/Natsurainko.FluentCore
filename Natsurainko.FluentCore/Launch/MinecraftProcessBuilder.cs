using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.GameManagement;
using Nrk.FluentCore.GameManagement.Dependencies;
using Nrk.FluentCore.GameManagement.Instances;
using Nrk.FluentCore.Launch;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Experimental.Launch;

/// <summary>
/// 参数构建器的默认实现
/// </summary>
public class MinecraftProcessBuilder
{
    private bool _isCmdMode = false;
    private bool _Dlog4j2_FormatMsgNoLookups = true;

    // Obtained from the GameInfo passed in the constructor
    private readonly string _nativesFolder;
    private readonly string _librariesFolder;
    private readonly string _assetsFolder;
    private string _gameDirectory;

    // Required, set by calling builder methods
    private IReadOnlyList<MinecraftLibrary>? _libraries;
    private IReadOnlyList<MinecraftLibrary>? _natives;

    private Account? _account;
    private string? _javaPath;
    private int? _minMemory;
    private int? _maxMemory;
    private bool _enableDemoUser = false;

    // 不可以将两个参数合并载入，两种参数要分别放在启动参数的对应位置才能被加载
    private List<string> _extraGameArguments = new();
    private List<string> _extraVmArguments = new();

    public MinecraftInstance MinecraftInstance { get; init; }

    public MinecraftProcessBuilder(MinecraftInstance gameInfo)
    {
        MinecraftInstance = gameInfo;
        _nativesFolder = Path.Combine(gameInfo.MinecraftFolderPath, "versions", gameInfo.InstanceId, "natives");
        _librariesFolder = Path.Combine(gameInfo.MinecraftFolderPath, "libraries");
        _assetsFolder = Path.Combine(gameInfo.MinecraftFolderPath, "assets");
        _gameDirectory = gameInfo.MinecraftFolderPath;

        SetLibraries();
    }

    public MinecraftProcess Build()
    {
        if (!CanBuild())
            throw new InvalidOperationException("Missing required parameters");

        if (_isCmdMode)
        {
#pragma warning disable CA1416
            return new MinecraftProcess(_javaPath, _gameDirectory, BuildArguments(), _natives, true);
#pragma warning restore CA1416
        }

        return new MinecraftProcess(_javaPath, _gameDirectory, BuildArguments(), _natives);
    }

    [MemberNotNullWhen(true, nameof(_libraries), nameof(_account), nameof(_javaPath), nameof(_minMemory), nameof(_maxMemory), nameof(_natives))]
    private bool CanBuild()
    {
        return _libraries != null && _account != null && _javaPath != null && _minMemory != null && _maxMemory != null;
    }

    /// <summary>
    /// 构建参数
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> BuildArguments()
    {
        // Null check before building arguments
        if (!CanBuild())
            throw new InvalidOperationException("Missing required parameters");

        if (MinecraftInstance.ClientJarPath is null)
            throw new InvalidOperationException("Invalid GameInfo");

        // Build arguments
        var versionJsonNode = JsonNode.Parse(File.ReadAllText(MinecraftInstance.ClientJsonPath))
            ?? throw new JsonException("Failed to parse version.json");
        var entity = versionJsonNode.Deserialize(MinecraftJsonSerializerContext.Default.ClientJsonObject)
            ?? throw new JsonException("Failed to parse version.json");

        var vmParameters = DefaultVmParameterParser.Parse(versionJsonNode);
        var gameParameters = DefaultGameParameterParser.Parse(versionJsonNode);

        if (MinecraftInstance is ModifiedMinecraftInstance { HasInheritance: true} inst)
        {
            var inheritedVersionJsonNode = JsonNode.Parse(File.ReadAllText(inst.InheritedMinecraftInstance.ClientJsonPath))
                ?? throw new JsonException("Failed to parse version.json");
            vmParameters = DefaultVmParameterParser
                .Parse(inheritedVersionJsonNode)
                .Union(vmParameters);
            gameParameters = DefaultGameParameterParser
                .Parse(inheritedVersionJsonNode)
                .Union(gameParameters);
        }

        var classPath = string.Join(Path.PathSeparator, _libraries.Select(lib => lib.FullPath));
        if (!string.IsNullOrEmpty(MinecraftInstance.ClientJarPath))
            classPath += Path.PathSeparator + MinecraftInstance.ClientJarPath;

        var vmParametersReplace = new Dictionary<string, string>()
        {
            { "${launcher_name}", "Natsurainko.FluentCore" },
            { "${launcher_version}", "4" },
            { "${classpath_separator}", Path.PathSeparator.ToString() },
            { "${library_directory}", _librariesFolder.ToPathParameter() },
            { "${natives_directory}", _nativesFolder.ToPathParameter() },
            { "${classpath}", classPath.ToPathParameter() },
            {
                "${version_name}", MinecraftInstance is ModifiedMinecraftInstance { HasInheritance: true } instance
                    ? instance.InheritedMinecraftInstance.InstanceId
                    : MinecraftInstance.InstanceId
            },
        };

        string? assetIndexPath = MinecraftInstance is ModifiedMinecraftInstance { HasInheritance: true } instance2 ?
            instance2.InheritedMinecraftInstance.AssetIndexJsonPath :
            MinecraftInstance.AssetIndexJsonPath;

        string assetIndexFilename = Path.GetFileNameWithoutExtension(assetIndexPath)
            ?? throw new InvalidOperationException("Invalid asset index path");

        string versionType = MinecraftInstance.Version.Type switch
        {
            MinecraftVersionType.Release => "release",
            MinecraftVersionType.Snapshot => "snapshot",
            MinecraftVersionType.OldBeta => "old_beta",
            MinecraftVersionType.OldAlpha => "old_alpha",
            _ => ""
        };

        var gameParametersReplace = new Dictionary<string, string>()
        {
            { "${auth_player_name}" , _account.Name },
            { "${auth_access_token}" , _account.AccessToken },
            { "${auth_session}" , _account.AccessToken },
            { "${auth_uuid}" ,_account.Uuid.ToString("N") },
            { "${user_type}" , _account.Type.Equals(AccountType.Microsoft) ? "MSA" : "Mojang" },
            { "${user_properties}" , "{}" },
            { "${version_name}" , MinecraftInstance.InstanceId.ToPathParameter() },
            { "${version_type}" , versionType },
            { "${game_assets}" , _assetsFolder.ToPathParameter() },
            { "${assets_root}" , _assetsFolder.ToPathParameter() },
            { "${game_directory}" , _gameDirectory.ToPathParameter() },
            { "${assets_index_name}" , assetIndexFilename },
        };

        var parentFolderPath = Directory.GetParent(MinecraftInstance.MinecraftFolderPath)?.FullName
            ?? throw new InvalidOperationException("Invalid Minecraft folder path"); // QUESTION: is this needed?

        if (_isCmdMode)
        {
            yield return "@echo off";
            yield return $"\r\nset APPDATA={parentFolderPath}";
            yield return $"\r\ncd /{MinecraftInstance.MinecraftFolderPath[0]} {MinecraftInstance.MinecraftFolderPath}";
            yield return $"\r\n{_javaPath.ToPathParameter()}";
        }

        yield return $"-Xms{_minMemory}M";
        yield return $"-Xmx{_maxMemory}M";
        yield return $"-Dminecraft.client.jar={MinecraftInstance.ClientJarPath.ToPathParameter()}";

        if (_Dlog4j2_FormatMsgNoLookups) yield return "-Dlog4j2.formatMsgNoLookups=true";

        foreach (var arg in DefaultVmParameterParser.GetEnvironmentJVMArguments()) yield return arg;
        foreach (var arg in vmParameters) yield return arg.ReplaceFromDictionary(vmParametersReplace);
        foreach (var arg in _extraVmArguments) yield return arg;

        yield return entity.MainClass!;

        foreach (var arg in gameParameters) yield return arg.ReplaceFromDictionary(gameParametersReplace);
        foreach (var arg in _extraGameArguments) yield return arg;

        if (_enableDemoUser) yield return "--demo";

        if (_isCmdMode) yield return "\r\npause";
    }

    /// <summary>
    /// 设置 Java [必须]
    /// </summary>
    /// <param name="javaPath">Java 可执行文件的绝对路径</param>
    /// <param name="maxMemory">最大虚拟机内存</param>
    /// <param name="minMemory">最小虚拟机内存</param>
    /// <returns></returns>
    public MinecraftProcessBuilder SetJavaSettings(string javaPath, int maxMemory, int minMemory)
    {
        _javaPath = javaPath;
        _minMemory = minMemory;
        _maxMemory = maxMemory;

        return this;
    }

    /// <summary>
    /// 设置 账户 [必须]
    /// </summary>
    /// <param name="account">用于游戏的账户</param>
    /// <param name="enableDemoUser">是否启用 Demo 模式</param>
    /// <returns></returns>
    public MinecraftProcessBuilder SetAccountSettings(Account account, bool enableDemoUser)
    {
        _account = account;
        _enableDemoUser = enableDemoUser;

        return this;
    }

    /// <summary>
    /// 设置 额外游戏参数 [可选]
    /// </summary>
    /// <param name="args">额外游戏参数</param>
    /// <returns></returns>
    public MinecraftProcessBuilder AddGameArguments(IEnumerable<string> args)
    {
        _extraGameArguments.AddRange(args);
        return this;
    }

    /// <summary>
    /// 设置 额外虚拟机参数 [可选]
    /// </summary>
    /// <param name="args">额外虚拟机参数</param>
    /// <returns></returns>
    public MinecraftProcessBuilder AddVmArguments(IEnumerable<string> args)
    {
        _extraVmArguments.AddRange(args);
        return this;
    }

    /// <summary>
    /// 设置 游戏运行目录 [可选]
    /// </summary>
    /// <param name="directory">游戏运行目录</param>
    /// <returns></returns>
    public MinecraftProcessBuilder SetGameDirectory(string directory)
    {
        _gameDirectory = directory;
        return this;
    }

    [SupportedOSPlatform("windows")]
    public MinecraftProcessBuilder SetCmdMode(bool isCmdMode)
    {
        _isCmdMode = isCmdMode;
        return this;
    }

    /// <summary>
    /// 设置 加载Libraries [必须]
    /// </summary>
    private void SetLibraries()
    {
        var libraries = new List<MinecraftLibrary>();
        var natives = new List<MinecraftLibrary>();

        if (MinecraftInstance is ModifiedMinecraftInstance { HasInheritance: true } modifiedMinecraftInstance)
        {
            (var inheritedLibs, var inheritedNatives) = modifiedMinecraftInstance.InheritedMinecraftInstance.GetRequiredLibraries();

            libraries.AddRange(inheritedLibs);
            natives.AddRange(inheritedNatives);
        }

        (var libs, var nats) = MinecraftInstance.GetRequiredLibraries();

        natives.AddRange(nats);

        foreach (var lib in libs)
        {
            MinecraftLibrary? existsEqualLib = null;
            MinecraftLibrary? sameNameLib = null;

            foreach (var containedLib in libraries)
            {
                if (lib.Equals(containedLib))
                {
                    existsEqualLib = containedLib;
                    break;
                }
                else if (lib.Name == containedLib.Name 
                    && lib.Classifier == containedLib.Classifier
                    && lib.Domain == lib.Domain)
                {
                    sameNameLib = containedLib;
                    break;
                }
            }

            if (existsEqualLib == null)
            {
                libraries.Add(lib);

                if (sameNameLib != null)
                    libraries.Remove(sameNameLib);
            }
        }

        _libraries = libraries;
        _natives = natives;
    }
}
