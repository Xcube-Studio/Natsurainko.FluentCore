﻿using Nrk.FluentCore.Management.ModLoaders;
using System;

namespace Nrk.FluentCore.Experimental.Management.Mods;

public record ModInfo
{
    public required string AbsolutePath { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Version { get; set; }

    public string[]? Authors { get; set; }

    public bool IsEnabled { get; set; }

    public ModLoaderType[] SupportedModLoaders { get; set; } = Array.Empty<ModLoaderType>();
}
