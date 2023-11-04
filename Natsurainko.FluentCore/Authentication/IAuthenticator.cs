namespace Nrk.FluentCore.Authentication;

/// <summary>
/// 验证器接口
/// </summary>
/// <typeparam name="TAccount"></typeparam>
public interface IAuthenticator<TAccount> where TAccount : Account
{
    TAccount Authenticate();
}
