namespace Nrk.FluentCore.Experimental.GameManagement;

public class MinecraftProfileBuilder
{
    private readonly string _minecraftFolderPath;

    public MinecraftProfileBuilder(string minecraftFolderPath)
    {
        _minecraftFolderPath = minecraftFolderPath;
    }

    public MinecraftProfile Build()
    {
        return new MinecraftProfile(_minecraftFolderPath);
    }
}