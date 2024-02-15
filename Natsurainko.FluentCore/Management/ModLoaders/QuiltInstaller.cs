using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Resources;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Management.ModLoaders;

public class QuiltInstaller : ModLoaderInstallerBase
{
    public required QuiltInstallBuild QuiltBuild { get; set; }

    public override Task<InstallResult> ExecuteAsync() =>
        Task.Run(() =>
            {
                ParseBuild(out JsonNode versionInfoJson, out IEnumerable<LibraryElement> libraries);

                DownloadLibraries(libraries);

                WriteFiles(versionInfoJson);

                OnProgressChanged(1.0);
            })
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                    return new InstallResult
                    {
                        Success = false,
                        Exception = task.Exception,
                        Log = null
                    };

                return new InstallResult
                {
                    Success = true,
                    Exception = null,
                    Log = null
                };
            });

    void ParseBuild(out JsonNode versionInfoJson, out IEnumerable<LibraryElement> libraries)
    {
        var responseMessage = HttpUtils.HttpGet(
            $"https://meta.quiltmc.org/v3/versions/loader/{InheritedFrom.AbsoluteId}/{QuiltBuild.BuildVersion}/profile/json"
        );
        var node = JsonNode.Parse(responseMessage.Content.ReadAsString()) ?? throw new Exception("Version info is null");
        versionInfoJson = node;

        var lib = (versionInfoJson["libraries"]?.AsArray()) ?? throw new Exception("Version info libraries not exist");

        libraries = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
            lib,
            InheritedFrom.MinecraftFolderPath
        );
    }

    void DownloadLibraries(IEnumerable<LibraryElement> libraries)
    {
        var resourcesDownloader = new DefaultResourcesDownloader(InheritedFrom);
        resourcesDownloader.SetLibraryElements(libraries);

        resourcesDownloader.Download();
    }

    void WriteFiles(JsonNode versionInfoJson)
    {
        if (!string.IsNullOrEmpty(AbsoluteId))
            versionInfoJson["id"] = AbsoluteId;

        var id = versionInfoJson["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Version ID is null or empty");

        var jsonFile = new FileInfo(Path.Combine(InheritedFrom.MinecraftFolderPath, "versions", id, $"{id}.json"));

        if (jsonFile.Directory is null)
            throw new Exception("Version directory is null");

        if (!jsonFile.Directory.Exists)
            jsonFile.Directory.Create();

        File.WriteAllText(jsonFile.FullName, versionInfoJson.ToString());
    }
}
