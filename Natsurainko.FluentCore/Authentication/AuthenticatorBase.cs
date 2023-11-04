namespace Nrk.FluentCore.Authentication;

/// <summary>
/// 验证器的抽象定义
/// </summary>
/// <typeparam name="TAccount"></typeparam>
public abstract class AuthenticatorBase<TAccount> : IAuthenticator<TAccount>
    where TAccount : Account
{
    /// <summary>
    /// 验证并取回账户
    /// </summary>
    /// <returns>成功完成验证的账户</returns>
    public abstract TAccount Authenticate();
}
