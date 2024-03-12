using Nrk.FluentCore.GameManagement.ModLoaders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public abstract class MinecraftInstance
{
    /// <summary>
    /// 游戏的绝对Id
    /// <para>
    /// 即 client.json 中的 "Id"
    /// </para>
    /// </summary>
    public required string Id { get; init; }

    public required MinecraftVersion Version { get; init; }

    /// <summary>
    /// 游戏是否为原版
    /// </summary>
    public bool IsVanilla { get => this is VanillaMinecraftInstance; }

    /// <summary>
    /// .minecraft 目录的绝对路径
    /// </summary>
    public required string MinecraftFolderPath { get; init; }

    /// <summary>
    /// client.json 的绝对路径
    /// </summary>
    public required string ClientJsonPath { get; init; }

    /// <summary>
    /// client.jar 的绝对路径
    /// </summary>
    public required string ClientJarPath { get; init; }

    /// <summary>
    /// assets\indexes\assetindex.json 的绝对路径（如果存在）
    /// </summary>
    public string? AssetsIndexJsonPath { get; init; }

    public static MinecraftInstance Parse(DirectoryInfo clientDir)
    {
        // Find client.json
        var clientJsonFile = clientDir
            .GetFiles($"{clientDir.Name}.json")
            .FirstOrDefault()
            ?? throw new FileNotFoundException($"client.json not found in {clientDir.FullName}");

        // Parse client.json
        string clientJson = File.ReadAllText(clientJsonFile.FullName);
        var clientJsonNode = JsonNode.Parse(clientJson)
            ?? throw new JsonException($"Failed to parse {clientJsonFile.FullName}");

        var clientJsonObject = clientJsonNode.Deserialize<ClientJsonObject>()
            ?? throw new JsonException($"Failed to deserialize {clientJsonFile.FullName} into {typeof(ClientJsonObject)}");

        // Create MinecraftInstance
        throw new NotImplementedException();
    }
}

public class VanillaMinecraftInstance : MinecraftInstance { }

/// <summary>
/// 模组加载器信息
/// </summary>
/// <param name="Type">加载器类型</param>
/// <param name="Version">加载器版本</param>
public record struct ModLoaderInfo(ModLoaderType Type, string Version);

public class ModifiedMinecraftInstance : MinecraftInstance
{
    /// <summary>
    /// 模组加载器信息列表
    /// </summary>
    public required IEnumerable<ModLoaderInfo> ModLoaders { get; init; }

    /// <summary>
    /// 是否有继承的核心
    /// </summary>
    [MemberNotNullWhen(true, nameof(InheritedMinecraftInstance))]
    public bool HasInheritence { get => InheritedMinecraftInstance is not null; }

    /// <summary>
    /// 继承的核心（若有）
    /// </summary>
    public MinecraftInstance? InheritedMinecraftInstance { get; init; }
}