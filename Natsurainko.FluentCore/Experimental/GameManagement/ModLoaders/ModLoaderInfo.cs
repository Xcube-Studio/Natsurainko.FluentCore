namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders;

/// <summary>
/// Mod loader information
/// </summary>
/// <param name="Type">Type of a mod loader</param>
/// <param name="Version">Version of a mod loader</param>
public record struct ModLoaderInfo(ModLoaderType Type, string Version);
