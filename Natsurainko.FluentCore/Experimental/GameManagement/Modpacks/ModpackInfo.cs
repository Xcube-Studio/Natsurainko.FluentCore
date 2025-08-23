using Nrk.FluentCore.GameManagement.Installer;

namespace Nrk.FluentCore.Experimental.GameManagement.Modpacks;

public enum ModpackType
{
    CurseForge,
    Modrinth,
    Unknown
}

public record ModpackInfo
{
    public required string McVersion { get; set; }

    public required ModLoaderInfo? ModLoader { get; set; }

    public required ModpackType ModpackType { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public string? Version { get; set; }

    public string? Author { get; set; }
}
