using Nrk.FluentCore.Launch;
using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Management.Parsing;

/// <summary>
/// 依赖库解析器的抽象定义
/// </summary>
public abstract class BaseLibraryParser
{
    protected readonly GameInfo _gameInfo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameInfo">要解析的游戏核心</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseLibraryParser(GameInfo gameInfo)
    {
        _gameInfo = gameInfo ?? throw new ArgumentNullException(nameof(gameInfo));
    }

    /// <summary>
    /// 遍历解析有效依赖库
    /// </summary>
    /// <param name="enabledLibraries">需调用的依赖库</param>
    /// <param name="enabledNativesLibraries">需解压的本地依赖库</param>
    public abstract void EnumerateLibraries(out IReadOnlyList<LibraryElement> enabledLibraries, out IReadOnlyList<LibraryElement> enabledNativesLibraries);
}
