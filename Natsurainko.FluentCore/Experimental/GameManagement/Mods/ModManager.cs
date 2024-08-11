using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Mods;

public class ModManager
{
    protected readonly string _modsFolder;

    private readonly List<(Exception, string)> _errorMods = [];
    public List<(Exception, string)> ErrorMods => _errorMods;

    public ModManager(string modsFolder)
    {
        _modsFolder = modsFolder;
    }

    public async IAsyncEnumerable<MinecraftMod> EnumerateModsAsync()
    {
        foreach (var file in Directory.EnumerateFiles(_modsFolder))
        {
            var fileExtension = Path.GetExtension(file);

            if (!(fileExtension.Equals(".jar") || fileExtension.Equals(".disabled")))
                continue;

            MinecraftMod modInfo = default!;

            await Task.Run(() => modInfo = ModInfoParser.Parse(file)).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    modInfo = new MinecraftMod
                    {
                        AbsolutePath = file,
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        IsEnabled = Path.GetExtension(file).Equals(".jar")
                    };
                }
            });

            yield return modInfo;
        }
    }

    public void Delete(MinecraftMod modInfo) => File.Delete(modInfo.AbsolutePath);

    public void Switch(MinecraftMod modInfo, bool isEnable)
    {
        var originalPath = modInfo.AbsolutePath;

        string parentPath =
            Path.GetDirectoryName(originalPath)
            ?? Path.GetPathRoot(originalPath) // The parent directory is null because the file is in the root directory
            ?? throw new InvalidDataException("ModInfo has an invalid absolute path");

        string newFileName = Path.GetFileNameWithoutExtension(originalPath) + (isEnable ? ".jar" : ".disabled");

        modInfo.AbsolutePath = Path.Combine(parentPath, newFileName);
        modInfo.IsEnabled = isEnable;
        File.Move(originalPath, modInfo.AbsolutePath);
    }
}
