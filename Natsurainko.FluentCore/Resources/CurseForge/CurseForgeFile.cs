using Nrk.FluentCore.GameManagement.Installer;
using System;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record CurseForgeFile
{
    [JsonPropertyName("fileId")]
    [JsonRequired]
    public int FileId { get; set; }

    [JsonPropertyName("gameVersion")]
    [JsonRequired]
    public string McVersion { get; set; } = null!;

    [JsonPropertyName("filename")]
    [JsonRequired]
    public string FileName { get; set; } = null!;

    [JsonPropertyName("modLoader")]
    public ModLoaderType ModLoaderType { get; set; }

    public int ModId { get; set; }
}

public record CurseForgeFileDetails
{
    [JsonRequired]
    [JsonPropertyName("id")] 
    public required int Id { get; set; }

    [JsonPropertyName("displayName")] 
    public required string DisplayName { get; init; }

    [JsonPropertyName("fileName")] 
    public required string FileName { get; init; }

    [JsonPropertyName("fileDate")] 
    public DateTime FileDate { get; set; }

    [JsonPropertyName("fileLength")] 
    public int FileLength { get; set; }

    [JsonPropertyName("releaseType")] 
    public int ReleaseType { get; set; }

    [JsonPropertyName("fileStatus")] 
    public int FileStatus { get; set; }

    [JsonPropertyName("downloadUrl")] 
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("isAlternate")] 
    public bool IsAlternate { get; set; }

    [JsonPropertyName("alternateFileId")] 
    public int AlternateFileId { get; set; }

    [JsonPropertyName("isAvailable")] 
    public bool IsAvailable { get; set; }

    [JsonPropertyName("modules")] 
    public ModuleJsonObject[]? Modules { get; set; }

    [JsonPropertyName("packageFingerprint")]
    public long PackageFingerprint { get; set; }

    [JsonPropertyName("fileFingerprint")] 
    public long FileFingerprint { get; set; }

    [JsonPropertyName("gameVersions")] 
    public required string[] GameVersions { get; init; }

    [JsonPropertyName("hasInstallScript")] 
    public bool HasInstallScript { get; set; }

    [JsonPropertyName("isCompatibleWithClient")]
    public bool IsCompatibleWithClient { get; set; }

    [JsonPropertyName("categorySectionPackageType")]
    public int CategorySectionPackageType { get; set; }

    [JsonPropertyName("restrictProjectFileAccess")]
    public int RestrictProjectFileAccess { get; set; }

    [JsonPropertyName("projectStatus")] 
    public int ProjectStatus { get; set; }

    [JsonPropertyName("projectId")] 
    public long ProjectId { get; set; }

    [JsonPropertyName("packageFingerprintId")]
    public long PackageFingerprintId { get; set; }

    [JsonPropertyName("gameId")] 
    public int GameId { get; set; }

    [JsonPropertyName("isServerPack")] 
    public bool IsServerPack { get; set; }

    public class ModuleJsonObject
    {
        [JsonPropertyName("foldername")] 
        public string? FolderName { get; set; }

        [JsonPropertyName("fingerprint")] 
        public long Fingerprint { get; set; }

        [JsonPropertyName("type")] 
        public int Type { get; set; }
    }
}