﻿using Nrk.FluentCore.GameManagement.Installer;
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
[JsonSerializable(typeof(IEnumerable<ClientJsonObject.LibraryJsonObject>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(IEnumerable<ForgeProcessorData>))] // ForgeInstanceInstaller
[JsonSerializable(typeof(IEnumerable<string>))] // ClientJsonObject
internal partial class MinecraftJsonSerializerContext : JsonSerializerContext
{
}
