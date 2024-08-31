using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Nrk.FluentCore.GameManagement.ClientJsonObject.ArgumentsJsonObject;

namespace Nrk.FluentCore.GameManagement;

[JsonSerializable(typeof(ClientJsonObject))]
[JsonSerializable(typeof(ConditionalClientArgument<GameArgumentRule>))]
[JsonSerializable(typeof(ConditionalClientArgument<ClientJsonObject.OsRule>))]
[JsonSerializable(typeof(IEnumerable<string>))]
internal partial class MinecraftJsonSerializerContext : JsonSerializerContext
{
}
