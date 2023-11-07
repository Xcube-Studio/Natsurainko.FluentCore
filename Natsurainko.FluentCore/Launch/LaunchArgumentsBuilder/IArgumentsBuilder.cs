using Nrk.FluentCore.Launch;
using System.Collections.Generic;

namespace Nrk.FluentCore.Launch;

/// <summary>
/// 参数生成器接口
/// </summary>
public interface IArgumentsBuilder
{
    /// <summary>
    /// 用于生成参数的游戏核心
    /// </summary>
    GameInfo GameInfo { get; }

    IEnumerable<string> Build();
}
