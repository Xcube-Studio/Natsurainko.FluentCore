using Nrk.FluentCore.GameManagement.Installer.Data.Modpack;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

[JsonSerializable(typeof(CurseForgeFile))]
[JsonSerializable(typeof(CurseForgeFileDetails))]
[JsonSerializable(typeof(ModrinthProject))]
[JsonSerializable(typeof(IEnumerable<ModrinthResource>))]
[JsonSerializable(typeof(CurseForgeModpackManifest))]
public partial class ResourcesJsonSerializerContext : JsonSerializerContext;
