using Nrk.FluentCore.Management.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

/// <summary>
/// Data structure of client.json in .minecraft/versions/&lt;version&gt;. It is named &lt;game version&gt;.json in later versions.
/// <para>More details on Minecraft Wiki: <seealso href="https://minecraft.wiki/w/Client.json"/></para>
/// </summary>
public class ClientJsonObject
{
    [JsonPropertyName("id")]
    public required string? Id { get; set; }

    [JsonPropertyName("mainClass")]
    public required string? MainClass { get; set; }

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
    /// Used by Forge (not part of vanilla Minecraft client.json)
    /// </summary>
    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }

    [JsonPropertyName("type")]
    public required string? Type { get; set; }

    [JsonPropertyName("assets")]
    public required string? Assets { get; set; }

    [JsonPropertyName("assetIndex")]
    public required AssstIndexJsonObject? AssetIndex { get; set; }

    /// <summary>
    /// client.json 下 arguments 键 对应的实体类
    /// </summary>
    public class ArgumentsJsonObject
    {
        [JsonPropertyName("game")]
        [JsonConverter(typeof(ClientArgumentsConverter<GameArgumentRule>))]
        public required IEnumerable<ClientArgument> GameArguments { get; set; } = [];

        [JsonPropertyName("jvm")]
        [JsonConverter(typeof(ClientArgumentsConverter<OsRule>))]
        public required IEnumerable<ClientArgument> JvmArguments { get; set; } = [];

        public abstract class ClientArgument { }

        public class DefaultClientArgument : ClientArgument
        {
            public required string Value { get; set; }
        }

        public class ConditionalClientArgument<TRule> : ClientArgument
        {
            [JsonPropertyName("value")]
            [JsonConverter(typeof(ArgumentValuesConverter))]
            public required IEnumerable<string> Values { get; set; }

            [JsonPropertyName("rules")]
            public required IEnumerable<TRule> Conditions { get; set; }
        }
        public class GameArgumentRule
        {
            [JsonPropertyName("action")]
            public required string Action { get; set; }

            [JsonPropertyName("features")]
            public required RuleFeatures Features { get; set; }

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

        public class OsRule
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("arch")]
            public string? Arch { get; set; }
        }

        internal class ClientArgumentsConverter<TRule> : JsonConverter<IEnumerable<ClientArgument>>
        {
            public override IEnumerable<ClientArgument> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
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
                        var obj = JsonSerializer.Deserialize<ConditionalClientArgument<TRule>>(ref reader, options);
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
                        JsonSerializer.Serialize(writer, conditionalArg, options);
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
                    var array = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader, options);
                    return array ?? [];
                }
                else
                {
                    throw new JsonException();
                }
            }

            public override void Write(Utf8JsonWriter writer, IEnumerable<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }

    /// <summary>
    /// client.json 下 assetIndex 键 对应的实体类
    /// </summary>
    public class AssstIndexJsonObject
    {
        [JsonPropertyName("url")]
        public required string Url { get; set; }

        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("sha1")]
        public required string Sha1 { get; set; }

        [JsonPropertyName("size")]
        public required int Size { get; set; }

        [JsonPropertyName("totalSize")]
        public required int TotalSize { get; set; }
    }
}
