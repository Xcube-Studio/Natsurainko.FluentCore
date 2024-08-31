using Nrk.FluentCore.GameManagement.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

[JsonSerializable(typeof(ClientJsonObject))]
[JsonSerializable(typeof(ClientJsonObject.ArgumentsJsonObject.ConditionalClientArgument<ClientJsonObject.ArgumentsJsonObject.GameArgumentRule>))]
[JsonSerializable(typeof(ClientJsonObject.ArgumentsJsonObject.ConditionalClientArgument<ClientJsonObject.OsRule>))]
[JsonSerializable(typeof(IEnumerable<ClientJsonObject.LibraryJsonObject>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(IEnumerable<ForgeProcessorData>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(IEnumerable<string>))] // ClientJsonObject
[JsonSerializable(typeof(Dictionary<string, AssetJsonNode>))] // MinecraftInstance
[JsonSerializable(typeof(ClientJsonObject.AssetIndexJsonObject))] // MinecraftInstance
internal partial class MinecraftJsonSerializerContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(OptiFineInstanceInstaller.OptiFineClientJson))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
internal partial class OptiFineInstallerJsonSerializerContext : JsonSerializerContext
{
}