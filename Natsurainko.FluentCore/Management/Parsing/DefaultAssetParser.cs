using Nrk.FluentCore.Launch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Management.Parsing;

/// <summary>
/// 依赖材质解析器的默认实现
/// </summary>
public class DefaultAssetParser : BaseAssetParser
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameInfo">要解析的游戏核心</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DefaultAssetParser(GameInfo gameInfo) : base(gameInfo) { }

    public override AssetElement GetAssetIndexJson()
    {
        // Parse version.json
        string versionJsonPath = _gameInfo.IsInheritedFrom ? _gameInfo.InheritsFrom.VersionJsonPath : _gameInfo.VersionJsonPath;
        JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(versionJsonPath));
        var assetIndex = jsonNode?["assetIndex"]?.Deserialize<AssstIndexJsonNode>();

        if (assetIndex is null)
            throw new InvalidDataException("Error in parsing version.json");

        // Create AssetElement
        var assetIndexFilePath = _gameInfo.IsInheritedFrom ? _gameInfo.InheritsFrom.AssetsIndexJsonPath : _gameInfo.AssetsIndexJsonPath;

        if (assetIndexFilePath is null)
            throw new InvalidDataException("Cannot find asset index file"); // QUESTION: does GameInfo guarantee that at least one of InheritsFrom and AssetsIndexJsonPath is not null?

        return new AssetElement
        {
            Name = assetIndex.Id + ".json",
            Checksum = assetIndex.Sha1,
            Url = assetIndex.Url,
            AbsolutePath = assetIndexFilePath,
            RelativePath = assetIndexFilePath.Replace(Path.Combine(_gameInfo.MinecraftFolderPath, "assets"), string.Empty).TrimStart('\\') // QUESTION: does this work for OS other than Windows?
        };
    }

    public override IEnumerable<AssetElement> EnumerateAssets()
    {
        var assetsIndexJsonPath = _gameInfo.IsInheritedFrom ? _gameInfo.InheritsFrom.AssetsIndexJsonPath : _gameInfo.AssetsIndexJsonPath;

        if (string.IsNullOrEmpty(assetsIndexJsonPath))
            yield break; //未找到 assets\indexes\assetindex.json

        // Parse assetindex.json
        var assets = JsonNode
            .Parse(File.ReadAllText(assetsIndexJsonPath))
            ?["objects"].Deserialize<Dictionary<string, AssetJsonNode>>();

        if (assets is null)
            throw new InvalidDataException("Error in parsing assetindex.json");

        // Parse AssetElement objects
        foreach (var keyValuePair in assets)
        {
            var hashPath = Path.Combine(keyValuePair.Value.Hash[..2], keyValuePair.Value.Hash);
            var relativePath = Path.Combine("objects", hashPath);
            var absolutePath = Path.Combine(_gameInfo.MinecraftFolderPath, "assets", relativePath);

            yield return new AssetElement
            {
                Name = keyValuePair.Key,
                Checksum = keyValuePair.Value.Hash,
                RelativePath = relativePath,
                AbsolutePath = absolutePath,
                Url = "https://resources.download.minecraft.net/" + hashPath.Replace('\\', '/')
            };
        }
    }
}
