using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Mods;

public static class ModManager
{
    public static async IAsyncEnumerable<MinecraftMod> EnumerateModsAsync(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var fileExtension = Path.GetExtension(file);

            if (!(fileExtension.Equals(".jar") || fileExtension.Equals(".disabled")))
                continue;

            if (!ModInfoParser.TryParse(file, out MinecraftMod? modInfo))
            {
                modInfo = new MinecraftMod
                {
                    AbsolutePath = file,
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    IsEnabled = Path.GetExtension(file).Equals(".jar")
                };
            }

            yield return await Task.FromResult(modInfo);
        }
    }
}
