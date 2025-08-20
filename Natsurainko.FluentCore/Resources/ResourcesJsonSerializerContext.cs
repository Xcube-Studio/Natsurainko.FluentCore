using Nrk.FluentCore.Resources.CurseForge;
using Nrk.FluentCore.Resources.Modrinth;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

[JsonSerializable(typeof(CurseForgeFile))]
[JsonSerializable(typeof(CurseForgeFileDetails))]
[JsonSerializable(typeof(ModrinthProject))]
[JsonSerializable(typeof(IEnumerable<ModrinthResource>))]
[JsonSerializable(typeof(CurseForgeModpackManifest))]
[JsonSerializable(typeof(ModrinthModpackManifest))]
public partial class ResourcesJsonSerializerContext : JsonSerializerContext;
