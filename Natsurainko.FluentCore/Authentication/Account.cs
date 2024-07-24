using System;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication;

/// <summary>
/// 游戏账户类型
/// </summary>
public enum AccountType
{
    /// <summary>
    /// 离线账户
    /// </summary>
    Offline = 0,
    /// <summary>
    /// 微软账户
    /// </summary>
    Microsoft = 1,
    /// <summary>
    /// Yggdrasil 第三方账户
    /// </summary>
    Yggdrasil = 2
}

/// <summary>
/// 表示一个账户
/// </summary>
/// <param name="Name">用户名</param>
/// <param name="Uuid">UUID</param>
/// <param name="AccessToken">AccessToken</param>
[JsonDerivedType(typeof(OfflineAccount), typeDiscriminator: "offline")]
[JsonDerivedType(typeof(MicrosoftAccount), typeDiscriminator: "microsoft")]
[JsonDerivedType(typeof(YggdrasilAccount), typeDiscriminator: "yggdrasil")]
public abstract record Account(string Name, Guid Uuid, string AccessToken)
{
    /// <summary>
    /// 账户类型
    /// </summary>
    public abstract AccountType Type { get; }
}

/// <summary>
/// 微软账户
/// </summary>
/// <param name="RefreshToken">RefreshToken</param>
/// <param name="LastRefreshTime">最后一次刷新时间</param>
public record MicrosoftAccount(
    string Name,
    Guid Uuid,
    string AccessToken,
    string RefreshToken,
    DateTime LastRefreshTime
) : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Microsoft;
}

/// <summary>
/// 外置账户
/// </summary>
/// <param name="YggdrasilServerUrl">外置验证服务器Url</param>
public record YggdrasilAccount(string Name, Guid Uuid, string AccessToken, string ClientToken, string YggdrasilServerUrl)
    : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Yggdrasil;
}

/// <summary>
/// 离线账户
/// </summary>
public record OfflineAccount(string Name, Guid Uuid, string AccessToken) : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Offline;
}
