using System;
using System.Collections.Generic;
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

    public string Name { get; init; } = Name;

    public Guid Uuid { get; init; } = Uuid;

    public string AccessToken { get; set; } = AccessToken;

    public virtual bool ProfileEquals(Account? account)
    {
        if (account == null)
            return false;

        if (account.Type.Equals(this.Type)
            && account.Uuid.Equals(this.Uuid)
            && account.Name.Equals(this.Name))
            return true;

        return false;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode() ^ Name.GetHashCode() ^ Uuid.GetHashCode();
    }
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
    DateTime LastRefreshTime) : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Microsoft;

    public DateTime LastRefreshTime { get; set; } = LastRefreshTime;

    public string RefreshToken { get; set; } = RefreshToken;

    public override bool ProfileEquals(Account? account)
    {
        if (account is MicrosoftAccount microsoftAccount
            && microsoftAccount.Uuid.Equals(this.Uuid))
            return true;

        return false;
    }

    public override int GetHashCode() => Type.GetHashCode() ^ Uuid.GetHashCode();
}

/// <summary>
/// 外置账户
/// </summary>
/// <param name="YggdrasilServerUrl">外置验证服务器Url</param>
public record YggdrasilAccount(
    string Name,
    Guid Uuid,
    string AccessToken,
    string YggdrasilServerUrl,
    string? ClientToken = default) : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Yggdrasil;

    public string YggdrasilServerUrl { get; set; } = YggdrasilServerUrl;

    [Obsolete("Use MetaData instead")]
    public string? ClientToken { get; set; } = ClientToken;

    public Dictionary<string, string> MetaData { get; set; } = [];

    public override bool ProfileEquals(Account? account)
    {
        if (account is YggdrasilAccount yggdrasilAccount
            && yggdrasilAccount.YggdrasilServerUrl.Equals(this.YggdrasilServerUrl)
            && yggdrasilAccount.Uuid.Equals(this.Uuid))
            return true;

        return false;
    }

    public override int GetHashCode() => Type.GetHashCode() ^ YggdrasilServerUrl.GetHashCode() ^ Uuid.GetHashCode();
}

/// <summary>
/// 离线账户
/// </summary>
public record OfflineAccount(
    string Name,
    Guid Uuid,
    string AccessToken) : Account(Name, Uuid, AccessToken)
{
    public override AccountType Type => AccountType.Offline;
}
