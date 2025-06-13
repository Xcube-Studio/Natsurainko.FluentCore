using Nrk.FluentCore.GameManagement.Installer;
using System.IO;

namespace Nrk.FluentCore.GameManagement.Mods;

public record MinecraftMod
{
    public required string AbsolutePath { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Version { get; set; }

    public string[]? Authors { get; set; }

    public bool IsEnabled { get; set; }

    public ModLoaderType[] SupportedModLoaders { get; set; } = [];
}

public static class MinecraftModExtensions
{
    public static void Delete(this MinecraftMod modInfo) => File.Delete(modInfo.AbsolutePath);

    public static void Switch(this MinecraftMod modInfo, bool isEnable)
    {
        var originalPath = modInfo.AbsolutePath;

        string parentPath =
            Path.GetDirectoryName(originalPath)
            ?? Path.GetPathRoot(originalPath) // The parent directory is null because the file is in the root directory
            ?? throw new InvalidDataException("ModInfo has an invalid absolute path");

        string newFilePath = Path.Combine(
            parentPath, 
            Path.GetFileNameWithoutExtension(originalPath) + (isEnable ? ".jar" : ".disabled"));

        File.Move(originalPath, newFilePath);

        modInfo.AbsolutePath = newFilePath;
        modInfo.IsEnabled = isEnable;
    }
}
