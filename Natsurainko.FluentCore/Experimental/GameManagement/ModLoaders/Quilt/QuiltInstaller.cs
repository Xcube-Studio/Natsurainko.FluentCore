using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders.Quilt;

public class QuiltInstaller : ModLoaderInstaller
{
    public required QuiltInstallBuild QuiltBuild { get; set; }

    private readonly IDownloader _downloader;

    public QuiltInstaller(IDownloader downloader)
    {
        _downloader = downloader;
    }

    public override Task<InstallationResult> ExecuteAsync() =>
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
                    return new InstallationResult
                    {
                        Success = false,
                        Exception = task.Exception,
                        Log = null
                    };

                return new InstallationResult
                {
                    Success = true,
                    Exception = null,
                    Log = null
                };
            });

    void ParseBuild(out JsonNode versionInfoJson, out IEnumerable<LibraryElement> libraries)
    {
        var responseMessage = HttpUtils.HttpGet(
            $"https://meta.quiltmc.org/v3/versions/loader/{InheritedInstance.VersionFolderName}/{QuiltBuild.BuildVersion}/profile/json"
        );
        var node = JsonNode.Parse(responseMessage.Content.ReadAsString()) ?? throw new Exception("Version info is null");
        versionInfoJson = node;

        var lib = (versionInfoJson["libraries"]?.AsArray()) ?? throw new Exception("Version info libraries not exist");

        libraries = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
            lib,
            InheritedInstance.MinecraftFolderPath
        );
    }

    void DownloadLibraries(IEnumerable<LibraryElement> libraries)
    {
        var requests = libraries.Select(lib => new DownloadRequest (lib.Url!, lib.AbsolutePath));
        var groupRequest = new GroupDownloadRequest(requests);
        _downloader.DownloadFilesAsync(groupRequest).GetAwaiter().GetResult();
    }

    void WriteFiles(JsonNode versionInfoJson)
    {
        if (!string.IsNullOrEmpty(AbsoluteId))
            versionInfoJson["id"] = AbsoluteId;

        var id = versionInfoJson["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Version ID is null or empty");

        var jsonFile = new FileInfo(Path.Combine(InheritedInstance.MinecraftFolderPath, "versions", id, $"{id}.json"));

        if (jsonFile.Directory is null)
            throw new Exception("Version directory is null");

        if (!jsonFile.Directory.Exists)
            jsonFile.Directory.Create();

        File.WriteAllText(jsonFile.FullName, versionInfoJson.ToString());
    }
}
