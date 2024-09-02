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
public abstract class Account(string name, Guid uuid, string accessToken)
{
    /// <summary>
    /// 账户类型
    /// </summary>
    public abstract AccountType Type { get; }

    public string Name { get; init; } = name;

    public Guid Uuid { get; init; } = uuid;

    public string AccessToken { get; set; } = accessToken;

    public override bool Equals(object? obj)
    {
        if (obj is Account account
            && account.Type.Equals(this.Type)
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
public class MicrosoftAccount(
    string name,
    Guid uuid,
    string accessToken,
    string refreshToken,
    DateTime lastRefreshTime) : Account(name, uuid, accessToken)
{
    public override AccountType Type => AccountType.Microsoft;

    public DateTime LastRefreshTime { get; set; } = lastRefreshTime;

    public string RefreshToken { get; set; } = refreshToken;

    public override bool Equals(object? obj)
    {
        if (obj is MicrosoftAccount microsoftAccount
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
public class YggdrasilAccount(
    string name, 
    Guid uuid, 
    string accessToken, 
    string clientToken, 
    string yggdrasilServerUrl): Account(name, uuid, accessToken)
{
    public override AccountType Type => AccountType.Yggdrasil;

    public string ClientToken { get; set; } = clientToken;

    public string YggdrasilServerUrl { get; set; } = yggdrasilServerUrl;

    public override bool Equals(object? obj)
    {
        if (obj is YggdrasilAccount yggdrasilAccount
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
public class OfflineAccount(
    string name, 
    Guid uuid, 
    string accessToken) : Account(name, uuid, accessToken)
{
    public override AccountType Type => AccountType.Offline;
}
