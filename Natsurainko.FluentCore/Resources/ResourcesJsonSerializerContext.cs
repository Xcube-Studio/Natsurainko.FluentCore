using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Resources;

[JsonSerializable(typeof(CurseForgeFile))]
[JsonSerializable(typeof(IEnumerable<ModrinthResource>))]
internal partial class ResourcesJsonSerializerContext : JsonSerializerContext
{
}
