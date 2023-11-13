namespace Nrk.FluentCore.Authentication;

/// <summary>
/// 验证器接口
/// </summary>
/// <typeparam name="TAccount">The type of accounts to be authenticated</typeparam>
public interface IAuthenticator<TAccount> where TAccount : Account
{
    /// <summary>
    /// 验证并取回账户
    /// </summary>
    /// <returns>成功完成验证的账户</returns>
    TAccount[] Authenticate();
}
