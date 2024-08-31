using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Nrk.FluentCore.GameManagement;

/// <summary>
/// Data structure of client.json in .minecraft/versions/&lt;version&gt;. It is named &lt;game version&gt;.json in later versions.
/// <para>More details on Minecraft Wiki: <seealso href="https://minecraft.wiki/w/Client.json"/></para>
/// </summary>
internal class ClientJsonObject
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string? Id { get; set; } = null;

    [JsonPropertyName("mainClass")]
    [JsonRequired]
    public string? MainClass { get; set; } = null;

    /// <summary>
    /// Replaced by <see cref="Arguments"/> since 1.13 (17w43a)
    /// </summary>
    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    /// <summary>
    /// Replaces <see cref="MinecraftArguments"/> since 1.13 (17w43a)
    /// </summary>
    [JsonPropertyName("arguments")]
    public ArgumentsJsonObject? Arguments { get; set; }

    /// <summary>
    /// Used by other mod loaders (not part of vanilla Minecraft client.json)
    /// </summary>
    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }

    [JsonPropertyName("type")]
    [JsonRequired]
    public string? Type { get; set; } = null;

    [JsonPropertyName("assets")]
    public string? Assets { get; set; }

    [JsonPropertyName("assetIndex")]
    public AssetIndexJsonObject? AssetIndex { get; set; }

    [JsonPropertyName("libraries")]
    [JsonRequired]
    public IEnumerable<LibraryJsonObject>? Libraries { get; set; } = null;

    /// <summary>
    /// client.json 下 arguments 键 对应的实体类
    /// </summary>
    internal class ArgumentsJsonObject
    {
        [JsonPropertyName("game")]
        [JsonConverter(typeof(ClientArgumentsConverter<GameArgumentRule>))]
        public IEnumerable<ClientArgument>? GameArguments { get; set; } = [];

        [JsonPropertyName("jvm")]
        [JsonConverter(typeof(ClientArgumentsConverter<OsRule>))]
        public IEnumerable<ClientArgument>? JvmArguments { get; set; } = [];

        public abstract class ClientArgument { }

        public class DefaultClientArgument : ClientArgument
        {
            public required string Value { get; set; }
        }

        public class ConditionalClientArgument<TRule> : ClientArgument
        {
            [JsonPropertyName("value")]
            [JsonConverter(typeof(ArgumentValuesConverter))]
            [JsonRequired]
            public IEnumerable<string>? Values { get; set; }

            [JsonPropertyName("rules")]
            [JsonRequired]
            public IEnumerable<TRule>? Conditions { get; set; }
        }
        public class GameArgumentRule
        {
            [JsonPropertyName("action")]
            [JsonRequired]
            public string Action { get; set; } = null!;

            [JsonPropertyName("features")]
            [JsonRequired]
            public RuleFeatures Features { get; set; } = null!;

            public class RuleFeatures
            {
                [JsonPropertyName("is_demo_user")]
                public bool? IsDemoUser { get; set; }

                [JsonPropertyName("has_custom_resolution")]
                public bool? HasCustomResolution { get; set; }

                [JsonPropertyName("has_quick_plays_support")]
                public bool? HasQuickPlaysSupport { get; set; }

                [JsonPropertyName("is_quick_play_singleplayer")]
                public bool? IsQuickPlaySingleplayer { get; set; }

                [JsonPropertyName("is_quick_play_multiplayer")]
                public bool? IsQuickPlayMultiplayer { get; set; }

                [JsonPropertyName("is_quick_play_realms")]
                public bool? IsQuickPlayRealms { get; set; }
            }
        }

        internal class ClientArgumentsConverter<TRule> : JsonConverter<IEnumerable<ClientArgument>>
        {
            public override IEnumerable<ClientArgument> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException("Unable to parse client argument");

                List<ClientArgument> arguments = new();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.String) // The argument is a string
                    {
                        string? value = reader.GetString();
                        if (value is not null)
                        {
                            arguments.Add(new DefaultClientArgument { Value = value });
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject) // The argument is a conditional argument
                    {
                        var obj =
                            JsonSerializer.Deserialize(ref reader, typeof(ConditionalClientArgument<TRule>), MinecraftJsonSerializerContext.Default)
                            as ConditionalClientArgument<TRule>;
                        if (obj is not null)
                        {
                            arguments.Add(obj);
                        }
                    }
                }

                return arguments;
            }

            public override void Write(Utf8JsonWriter writer, IEnumerable<ClientArgument> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                foreach (var arg in value)
                {
                    if (arg is DefaultClientArgument defaultArg)
                    {
                        writer.WriteStringValue(defaultArg.Value);
                    }
                    else if (arg is ConditionalClientArgument<TRule> conditionalArg)
                    {
                        JsonSerializer.Serialize(writer, conditionalArg, typeof(ConditionalClientArgument<TRule>), MinecraftJsonSerializerContext.Default);
                    }
                }
                writer.WriteEndArray();
            }
        }

        internal class ArgumentValuesConverter : JsonConverter<IEnumerable<string>>
        {
            public override IEnumerable<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string? value = reader.GetString();
                    return value is null ? [] : [value];
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var array =
                        JsonSerializer.Deserialize(ref reader, typeof(IEnumerable<string>), MinecraftJsonSerializerContext.Default)
                        as IEnumerable<string>;
                    return array ?? [];
                }
                else
                {
                    throw new JsonException();
                }
            }

            public override void Write(Utf8JsonWriter writer, IEnumerable<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, typeof(IEnumerable<string>), MinecraftJsonSerializerContext.Default);
            }
        }
    }

    /// <summary>
    /// client.json 下 assetIndex 键 对应的实体类
    /// </summary>
    internal class AssetIndexJsonObject
    {
        [JsonPropertyName("url")]
        [JsonRequired]
        public string? Url { get; set; }

        [JsonPropertyName("id")]
        [JsonRequired]
        public string? Id { get; set; }

        [JsonPropertyName("sha1")]
        [JsonRequired]
        public string? Sha1 { get; set; }

        [JsonPropertyName("size")]
        [JsonRequired]
        public int? Size { get; set; }

        [JsonPropertyName("totalSize")]
        [JsonRequired]
        public int? TotalSize { get; set; }
    }

    internal class LibraryJsonObject
    {
        [JsonPropertyName("name")]
        [JsonRequired]
        public string? MavenName { get; set; }

        // Used by Forge, Fabric, Quilt..
        [JsonPropertyName("url")]
        public string? MavenUrl { get; set; }

        // Used by Fabric
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        // Used by Fabric
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        // Used by Forge
        [JsonPropertyName("clientreq")]
        public bool? ClientRequest { get; set; }

        // Used by Forge
        [JsonPropertyName("serverreq")]
        public bool? ServerRequest { get; set; }

        // This field may not exist for libraries used by mod loaders
        [JsonPropertyName("downloads")]
        public DownloadInformationJsonObject? DownloadInformation { get; set; }

        [JsonPropertyName("rules")]
        public IEnumerable<OsRule>? Rules { get; set; }

        // "platform-name": "classifier"
        // Classifier is used for identifying native libraries
        [JsonPropertyName("natives")]
        public Dictionary<string, string>? NativeClassifierNames { get; set; }

        public class DownloadInformationJsonObject
        {
            [JsonPropertyName("artifact")]
            public DownloadArtifactJsonObject? Artifact { get; set; }

            // Possible keys: "javadoc", "sources" and "natives-*"
            // Keys for native libraries are declared in the "natives" field in LibraryJsonObject
            [JsonPropertyName("classifiers")]
            public Dictionary<string, DownloadArtifactJsonObject>? Classifiers { get; set; }
        }

        public class DownloadArtifactJsonObject
        {
            [JsonPropertyName("path")]
            [JsonRequired]
            public string? Path { get; set; }

            [JsonPropertyName("url")]
            [JsonRequired]
            public string? Url { get; set; }

            [JsonPropertyName("sha1")]
            [JsonRequired]
            public string? Sha1 { get; set; }

            [JsonPropertyName("size")]
            [JsonRequired]
            public long? Size { get; set; }
        }
    }

    internal class OsRule
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("os")]
        public Os? Os { get; set; }
    }

    internal class Os
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("arch")]
        public string? Arch { get; set; }
    }
}

public record AssetJsonNode
{
    [JsonPropertyName("hash")]
    [JsonRequired]
    public string? Hash { get; set; }

    [JsonPropertyName("size")]
    [JsonRequired]
    public int? Size { get; set; }
}