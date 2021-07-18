using FluentCore.UWP.Interface;
using FluentCore.UWP.Model.Launch;
using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Service.Component.DependencesResolver
{
    public class LibrariesResolver : IDependencesResolver
    {
        public GameCore GameCore { get; set; }

        public LibrariesResolver(GameCore core) => this.GameCore = core;

        public IEnumerable<IDependence> GetDependences() 
        {
            foreach (var lib in this.GameCore.Libraries)
                yield return lib;
            foreach (var native in this.GameCore.Natives)
                yield return native;
        }

        public IEnumerable<IDependence> GetLostDependences()
        {
            foreach (var lib in this.GameCore.Libraries)
            {
                var file = new FileInfo($"{PathHelper.GetLibrariesFolder(GameCore.Root)}\\{lib.GetRelativePath()}");

                if (!file.Exists)
                    yield return lib;

                if (lib.Downloads != null && lib.Downloads.Artifact != null && !FileHelper.FileVerify(file, lib.Downloads.Artifact.Size, lib.Downloads.Artifact.Sha1))
                    yield return lib;
            }

            foreach (var native in this.GameCore.Natives)
            {
                var file = new FileInfo($"{PathHelper.GetLibrariesFolder(GameCore.Root)}\\{native.GetRelativePath()}");
                var model = native.Downloads.Classifiers[native.Natives["windows"].Replace("${arch}", SystemConfiguration.Arch)];

                if (!file.Exists)
                    yield return native;

                if (!FileHelper.FileVerify(file, model.Size, model.Sha1))
                    yield return native;
            }
        }
    }
}
