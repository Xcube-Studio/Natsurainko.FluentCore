namespace Natsurainko.FluentCore.Model.Install;

public class ModLoaderInformation
{
    public ModLoaderType LoaderType { get; set; }

    public string Version { get; set; }
}

public enum ModLoaderType
{
    Forge = 1,
    Cauldron = 2,
    LiteLoader = 3,
    Fabric = 4,
    OptiFine = 6,
    Unknown = 7,
}
