using NbtToolkit.Binary;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Saves;

public static class SaveInfoParser
{
    public static async Task<SaveInfo> ParseAsync(string saveFolder)
    {
        using var fileStream = new FileStream(Path.Combine(saveFolder, "level.dat"), FileMode.Open, FileAccess.Read);
        using var _nbtReader = new NbtReader(fileStream, NbtCompression.GZip, true);

        var rootTag = _nbtReader.ReadRootTag();
        var dataTagCompound = rootTag["Data"].AsTagCompound();

        SaveInfo saveInfo = new()
        {
            FolderName = new DirectoryInfo(saveFolder).Name,
            Folder = saveFolder,
            LevelName = dataTagCompound["LevelName"].AsString(),
            AllowCommands = dataTagCompound["allowCommands"].AsBool(),
            GameType = dataTagCompound["GameType"].AsInt(),
            Version = dataTagCompound["Version"].AsTagCompound()["Name"].AsString(),
            LastPlayed = DateTimeOffset.FromUnixTimeMilliseconds(dataTagCompound["LastPlayed"].AsLong()).ToLocalTime().DateTime
        };

        if (dataTagCompound.ContainsKey("WorldGenSettings"))
            saveInfo.Seed = dataTagCompound["WorldGenSettings"].AsTagCompound()["seed"].AsLong();
        else if (dataTagCompound.ContainsKey("RandomSeed"))
            saveInfo.Seed = dataTagCompound["RandomSeed"].AsLong();

        if (File.Exists(Path.Combine(saveFolder, "icon.png")))
            saveInfo.IconFilePath = Path.Combine(saveFolder, "icon.png");

        return await Task.FromResult(saveInfo);
    }
}
