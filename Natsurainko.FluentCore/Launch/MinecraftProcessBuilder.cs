using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Launch;

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
    private IEnumerable<LibraryElement>? _libraries;
    private Account? _account;
    private string? _javaPath;
    private int? _minMemory;
    private int? _maxMemory;
    private bool _enableDemoUser = false;
    private List<string> _extraArguments = new();

    public GameInfo GameInfo { get; init; }

    public MinecraftProcessBuilder(GameInfo gameInfo)
    {
        GameInfo = gameInfo;
        _nativesFolder = Path.Combine(gameInfo.MinecraftFolderPath, "versions", gameInfo.AbsoluteId, "natives");
        _librariesFolder = Path.Combine(gameInfo.MinecraftFolderPath, "libraries");
        _assetsFolder = Path.Combine(gameInfo.MinecraftFolderPath, "assets");
        _gameDirectory = gameInfo.MinecraftFolderPath;
    }

    public MinecraftProcess Build()
    {
        if (!CanBuild())
            throw new InvalidOperationException("Missing required parameters");

        return new MinecraftProcess(_javaPath, _gameDirectory, BuildArguments());
    }

    [MemberNotNullWhen(true, nameof(_libraries), nameof(_account), nameof(_javaPath), nameof(_minMemory), nameof(_maxMemory))]
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
        // Null check before building argumetns
        if (!CanBuild())
            throw new InvalidOperationException("Missing required parameters");

        if (GameInfo.JarPath is null)
            throw new InvalidOperationException("Invalid GameInfo");

        // Build arguments
        var versionJsonNode = JsonNode.Parse(File.ReadAllText(GameInfo.VersionJsonPath))
            ?? throw new JsonException("Failed to parse version.json");
        var entity = versionJsonNode.Deserialize<VersionJsonEntity>()
            ?? throw new JsonException("Failed to parse version.json");

        var vmParameters = DefaultVmParameterParser.Parse(versionJsonNode);
        var gameParameters = DefaultGameParameterParser.Parse(versionJsonNode);

        if (GameInfo.IsInheritedFrom)
        {
            var inheritedVersionJsonNode = JsonNode.Parse(File.ReadAllText(GameInfo.InheritsFrom.VersionJsonPath))
                ?? throw new JsonException("Failed to parse version.json");
            vmParameters = DefaultVmParameterParser
                .Parse(inheritedVersionJsonNode)
                .Union(vmParameters);
            gameParameters = DefaultGameParameterParser
                .Parse(inheritedVersionJsonNode)
                .Union(gameParameters);
        }

        var classPath = string.Join(Path.PathSeparator, _libraries.Select(lib => lib.AbsolutePath));
        if (!string.IsNullOrEmpty(GameInfo.JarPath)) classPath += Path.PathSeparator + GameInfo.JarPath;

        var vmParametersReplace = new Dictionary<string, string>()
        {
            { "${launcher_name}", "Natsurainko.FluentCore" },
            { "${launcher_version}", "4" },
            { "${classpath_separator}", Path.PathSeparator.ToString() },
            { "${library_directory}", _librariesFolder.ToPathParameter() },
            { "${natives_directory}", _nativesFolder.ToPathParameter() },
            { "${classpath}", classPath.ToPathParameter() },
            {
                "${version_name}", GameInfo.IsInheritedFrom
                ? GameInfo.InheritsFrom.AbsoluteId
                : GameInfo.AbsoluteId
            },
        };

        string? assetIndexPath = GameInfo.IsInheritedFrom ?
            GameInfo.InheritsFrom.AssetsIndexJsonPath :
            GameInfo.AssetsIndexJsonPath;

        string assetIndexFilename = Path.GetFileNameWithoutExtension(assetIndexPath)
            ?? throw new InvalidOperationException("Invalid asset index path");

        var gameParametersReplace = new Dictionary<string, string>()
        {
            { "${auth_player_name}" , _account.Name },
            { "${auth_access_token}" , _account.AccessToken },
            { "${auth_session}" , _account.AccessToken },
            { "${auth_uuid}" ,_account.Uuid.ToString("N") },
            { "${user_type}" , _account.Type.Equals(AccountType.Microsoft) ? "MSA" : "Mojang" },
            { "${user_properties}" , "{}" },
            { "${version_name}" , GameInfo.AbsoluteId },
            { "${version_type}" , GameInfo.Type },
            { "${game_assets}" , _assetsFolder.ToPathParameter() },
            { "${assets_root}" , _assetsFolder.ToPathParameter() },
            { "${game_directory}" , _gameDirectory.ToPathParameter() },
            { "${assets_index_name}" , assetIndexFilename },
        };

        var parentFolderPath = Directory.GetParent(GameInfo.MinecraftFolderPath)?.FullName
            ?? throw new InvalidOperationException("Invalid Minecraft folder path"); // QUESTION: is this needed?

        if (_isCmdMode)
        {
            yield return "@echo off";
            yield return $"\r\nset APPDATA={parentFolderPath}";
            yield return $"\r\ncd /{GameInfo.MinecraftFolderPath[0]} {GameInfo.MinecraftFolderPath}";
            yield return $"\r\n{_javaPath.ToPathParameter()}";
        }

        yield return $"-Xms{_minMemory}M";
        yield return $"-Xmx{_maxMemory}M";
        yield return $"-Dminecraft.client.jar={GameInfo.JarPath.ToPathParameter()}";

        if (_Dlog4j2_FormatMsgNoLookups) yield return "-Dlog4j2.formatMsgNoLookups=true";

        foreach (var arg in DefaultVmParameterParser.GetEnvironmentJVMArguments()) yield return arg;
        foreach (var arg in vmParameters) yield return arg.ReplaceFromDictionary(vmParametersReplace);

        yield return entity.MainClass;

        foreach (var arg in gameParameters) yield return arg.ReplaceFromDictionary(gameParametersReplace);

        foreach (var arg in _extraArguments)
            yield return arg;

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
    /// 设置 加载Libraries [必须]
    /// </summary>
    /// <param name="libraryElements"></param>
    /// <returns></returns>
    public MinecraftProcessBuilder SetLibraries(IEnumerable<LibraryElement> libraryElements)
    {
        _libraries = libraryElements;
        return this;
    }

    /// <summary>
    /// 设置 游戏额外参数 [可选]
    /// </summary>
    /// <param name="extraVmParameters">额外虚拟机参数</param>
    /// <param name="extraGameParameters">额外游戏参数</param>
    /// <returns></returns>
    public MinecraftProcessBuilder AddArguments(IEnumerable<string> args)
    {
        _extraArguments.AddRange(args);
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

    public MinecraftProcessBuilder SetCmdMode(bool isCmdMode)
    {
        _isCmdMode = isCmdMode;
        return this;
    }
}
