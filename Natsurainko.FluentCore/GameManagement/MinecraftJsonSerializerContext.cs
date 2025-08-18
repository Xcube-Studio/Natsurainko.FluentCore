using Nrk.FluentCore.GameManagement.Installer;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement;

[JsonSerializable(typeof(ClientJsonObject))]
[JsonSerializable(typeof(ClientJsonObject.ArgumentsJsonObject.ConditionalClientArgument<ClientJsonObject.ArgumentsJsonObject.GameArgumentRule>))]
[JsonSerializable(typeof(ClientJsonObject.ArgumentsJsonObject.ConditionalClientArgument<ClientJsonObject.OsRule>))]
[JsonSerializable(typeof(IEnumerable<ClientJsonObject.LibraryJsonObject>))]
[JsonSerializable(typeof(IEnumerable<string>))] // ClientJsonObject
[JsonSerializable(typeof(VersionManifestJsonObject))]
[JsonSerializable(typeof(Dictionary<string, AssetJsonNode>))] // MinecraftInstance
[JsonSerializable(typeof(ClientJsonObject.AssetIndexJsonObject))] // MinecraftInstance
internal partial class MinecraftJsonSerializerContext : JsonSerializerContext;

[JsonSerializable(typeof(OptiFineInstallData[]))]
[JsonSerializable(typeof(OptiFineInstanceInstaller.OptiFineClientJson))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
internal partial class OptiFineInstallerJsonSerializerContext : JsonSerializerContext;

[JsonSerializable(typeof(ForgeInstallData[]))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(IEnumerable<ForgeInstanceInstaller.ForgeProcessorData>))] // ForgeInstanceInstaller
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
internal partial class ForgeInstallerJsonSerializerContext : JsonSerializerContext;

[JsonSerializable(typeof(FabricInstallData[]))]
internal partial class FabricInstallerJsonSerializerContext : JsonSerializerContext;

[JsonSerializable(typeof(QuiltInstallData[]))]
internal partial class QuiltInstallerJsonSerializerContext : JsonSerializerContext;