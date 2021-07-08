using FluentCore.Model.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Local
{
    public class NativesDecompressor
    {
        public NativesDecompressor(string root, string id) 
        {
            this.Root = root;
            this.Id = id;
        }

        public string Root { get; set; }

        public string Id { get; set; }

        public void Decompress(IEnumerable<Native> natives, string nativesFolder = null)
        {
            nativesFolder = string.IsNullOrEmpty(nativesFolder) ? $"{PathHelper.GetVersionFolder(Root, Id)}{PathHelper.X}natives" : nativesFolder;

            if (!Directory.Exists(nativesFolder))
                Directory.CreateDirectory(nativesFolder);

            foreach(var item in natives)
                using (ZipArchive zip = ZipFile.OpenRead($"{PathHelper.GetLibrariesFolder(Root)}{PathHelper.X}{item.GetRelativePath()}"))
                    foreach (ZipArchiveEntry entry in zip.Entries)
                        if (entry.FullName.Contains(".dll"))
                            entry.ExtractToFile($"{nativesFolder}{PathHelper.X}{entry.Name}", true);
        }
    }
}
