using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders.Quilt;

public class QuiltInstaller : ModLoaderInstaller
{
    public required QuiltInstallData QuiltBuild { get; set; }

    private readonly IDownloader _downloader;

    public QuiltInstaller(IDownloader downloader)
    {
        _downloader = downloader;
    }

    public override Task<InstallationResult> ExecuteAsync() =>
        Task.Run(() =>
            {
                ParseBuild(out JsonNode versionInfoJson, out IEnumerable<MinecraftLibrary> libraries);

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

    void ParseBuild(out JsonNode clientJsonNode, out IEnumerable<MinecraftLibrary> libraries)
    {
        var responseMessage = HttpUtils.HttpGet(
            $"https://meta.quiltmc.org/v3/versions/loader/{InheritedInstance.InstanceId}/{QuiltBuild.BuildVersion}/profile/json"
        );
        var node = JsonNode.Parse(responseMessage.Content.ReadAsString()) ?? throw new Exception("Version info is null");
        clientJsonNode = node;

        libraries = (clientJsonNode["libraries"] as JsonArray)?
            .Deserialize<IEnumerable<ClientJsonObject.LibraryJsonObject>>()?
            .Select(lib => MinecraftLibrary.ParseJsonNode(lib, InheritedInstance.MinecraftFolderPath))
            ?? throw new InvalidDataException("Error in parsing version info json");
    }

    void DownloadLibraries(IEnumerable<MinecraftLibrary> libraries)
    {
        var requests = libraries.Select(lib => new DownloadRequest (lib.Url!, lib.FullPath));
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
