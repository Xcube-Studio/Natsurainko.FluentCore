namespace Nrk.FluentCore.Environment;

/// <summary>
/// 内存使用情况
/// </summary>
public record MemoryMetrics
{
    /// <summary>
    /// 总计内存（MB）
    /// </summary>
    public required double Total { get; init; }

    /// <summary>
    /// 已使用内存（MB）
    /// </summary>
    public required double Used { get; init; }

    /// <summary>
    /// 空闲内存（MB）
    /// </summary>
    public required double Free { get; init; }
}
