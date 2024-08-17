﻿namespace Nrk.FluentCore.Management.ModLoaders;

/// <summary>
/// Type of a mod loader
/// </summary>
public enum ModLoaderType
{
    Any = 0,
    Forge = 1,
    Cauldron = 2,
    LiteLoader = 3,
    Fabric = 4,
    Quilt = 5,
    NeoForge = 6,
    OptiFine = 7,
    Unknown,
}
