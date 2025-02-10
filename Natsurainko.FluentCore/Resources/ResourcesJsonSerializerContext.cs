using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

[JsonSerializable(typeof(CurseForgeFile))]
[JsonSerializable(typeof(ModrinthProject))]
[JsonSerializable(typeof(IEnumerable<ModrinthResource>))]
internal partial class ResourcesJsonSerializerContext : JsonSerializerContext
{
}
