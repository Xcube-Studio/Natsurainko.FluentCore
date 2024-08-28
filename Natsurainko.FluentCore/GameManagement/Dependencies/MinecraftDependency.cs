﻿using System.IO;

namespace Nrk.FluentCore.GameManagement.Dependencies;

public abstract class MinecraftDependency // TODO: Implement IDownloadable interface for downloading game dependencies
{
    /// <summary>
    /// Absolute path of the .minecraft folder
    /// </summary>
    public required string MinecraftFolderPath { get; init; }

    /// <summary>
    /// File path relative to the .minecraft folder
    /// </summary>
    public abstract string FilePath { get; } // Can be generated by derived types from their properties

    /// <summary>
    /// Absolute path of the file
    /// </summary>
    public string FullPath => Path.Combine(MinecraftFolderPath, FilePath);
}