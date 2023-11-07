using System;
using System.Collections.Generic;
using System.IO;

namespace Nrk.FluentCore.Management;

public class DefaultModsManager : BaseModsManager
{
    private readonly List<(Exception, string)> _errorMods = new();
    public List<(Exception, string)> ErrorMods => _errorMods;

    public DefaultModsManager(string modsFolder)
        : base(modsFolder) { }

    public override IEnumerable<ModInfo> EnumerateMods()
    {
        foreach (var file in Directory.EnumerateFiles(_modsFolder))
        {
            var fileExtension = Path.GetExtension(file);

            if (!(fileExtension.Equals(".jar") || fileExtension.Equals(".disabled")))
                continue;

            ModInfo? modInfo = null;

            try
            {
                modInfo = DefaultModInfoParser.Parse(file);
            }
            catch (Exception ex)
            {
                _errorMods.Add((ex, file));

                modInfo = new ModInfo
                {
                    AbsolutePath = file,
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    IsEnabled = Path.GetExtension(file).Equals(".jar")
                };
            }

            yield return modInfo;
        }
    }

    public override void Delete(ModInfo modInfo) => File.Delete(modInfo.AbsolutePath);

    public override void Switch(ModInfo modInfo, bool isEnable)
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
