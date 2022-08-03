using Natsurainko.FluentCore.Class.Model.Download;
using Natsurainko.FluentCore.Class.Model.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Module.Parser
{
    public class AssetParser
    {
        public AssetManifestJsonEntity Entity { get; set; }

        public DirectoryInfo Root { get; set; }

        public AssetParser(AssetManifestJsonEntity jsonEntity, DirectoryInfo directoryInfo)
        {
            this.Entity = jsonEntity;
            this.Root = directoryInfo;
        }

        public IEnumerable<AssetResource> GetAssets()
        {
            foreach (var (name, entity) in Entity.Objects)
                yield return new AssetResource
                {
                    Name = name,
                    CheckSum = entity.Hash,
                    Size = entity.Size,
                    Root = this.Root
                };
        }
    }
}
