using Nrk.FluentCore.GameManagement.Installer;
using System;

namespace Nrk.FluentCore.GameManagement.Mods;

public record MinecraftMod
{
    public required string AbsolutePath { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Version { get; set; }

    public string[]? Authors { get; set; }

    public bool IsEnabled { get; set; }

    public ModLoaderType[] SupportedModLoaders { get; set; } = Array.Empty<ModLoaderType>();
}
