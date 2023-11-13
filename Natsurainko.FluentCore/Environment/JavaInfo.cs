using System;

namespace Nrk.FluentCore.Environment;

/// <summary>
/// 表示 Java 基本信息
/// </summary>
public record JavaInfo
{
    /// <summary>
    /// 版本
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// 发行
    /// </summary>
    public required string? Company { get; init; }

    public required string? ProductName { get; init; }

    /// <summary>
    /// 架构体系
    /// </summary>
    public required string Architecture { get; init; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public required string Name { get; init; }
}
