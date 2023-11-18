﻿using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Management.Parsing;

/// <summary>
/// 默认游戏参数解析器
/// </summary>
public static class DefaultGameParameterParser
{
    /// <summary>
    /// 解析
    /// </summary>
    /// <param name="jsonNode"></param>
    /// <returns></returns>
    public static IEnumerable<string> Parse(JsonNode jsonNode)
    {
        var jsonGame = jsonNode["arguments"]?["game"];
        var jsonMinecraftArguments = jsonNode["minecraftArguments"];

        if (jsonMinecraftArguments != null && !string.IsNullOrEmpty(jsonMinecraftArguments.GetValue<string>()))
            foreach (var arg in StringExtensions.ArgumnetsGroup(jsonMinecraftArguments.GetValue<string>().Split(' ')))
                yield return arg;

        if (jsonGame is null)
            yield break;

        var list = StringExtensions.ArgumnetsGroup(jsonGame.AsArray()
            .Where(x => x is JsonValue)
            .WhereNotNull()
            .Select(x => x.GetValue<string>()));

        foreach (var item in list)
            yield return item;
    }
}
