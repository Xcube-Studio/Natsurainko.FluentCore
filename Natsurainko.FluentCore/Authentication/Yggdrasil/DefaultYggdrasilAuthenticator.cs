using Nrk.FluentCore.Utils;
using System;
using System.Linq;
using System.Text.Json;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class DefaultYggdrasilAuthenticator : IAuthenticator<YggdrasilAccount>
{
    private string _method;

    // Common
    private string _yggdrasilServerUrl;
    private string _clientToken = Guid.NewGuid().ToString("N");

    // For login
    private string? _email;
    private string? _password;

    // For refresh
    private string? _accessToken;

    /// <summary>
    /// Authenticate with Yggdrasil
    /// </summary>
    /// <returns>Authenticated Yggdrasil accounts</returns>
    /// <exception cref="YggdrasilAuthenticationException"></exception>
    public YggdrasilAccount[] Authenticate()
    {
        string url = _yggdrasilServerUrl;
        string content = string.Empty;

        switch (_method)
        {
            case "authenticate":
                url += "/authserver/authenticate";
                content = JsonSerializer.Serialize(
                    new YggdrasilLoginRequest
                    {
                        ClientToken = _clientToken,
                        UserName = _email!, // not null when method is "authenticate"
                        Password = _password! // not null when method is "authenticate"
                    }
                );
                break;
            case "refresh":
                url += "/authserver/refresh";
                content = JsonSerializer.Serialize(
                    new YggdrasilRefreshRequest
                    {
                        ClientToken = _clientToken,
                        AccessToken = _accessToken!, // not null when method is "refresh"
                        RequestUser = true
                    }
                );
                break;
        }

        using var res = HttpUtils.HttpPost(url, content);
        string result = res.Content.ReadAsString();

        res.EnsureSuccessStatusCode();

        var model = JsonSerializer.Deserialize<YggdrasilResponseModel>(result);

        if (model?.AvailableProfiles is null)
            throw new YggdrasilAuthenticationException(result);

        return model.AvailableProfiles
            .Select(profile =>
            {
                if (profile.Name is null || profile.Id is null || model.AccessToken is null)
                    throw new YggdrasilAuthenticationException(result);

                return new YggdrasilAccount(
                    Name: profile.Name,
                    Uuid: Guid.Parse(profile.Id),
                    AccessToken: model.AccessToken,
                    ClientToken: _clientToken,
                    YggdrasilServerUrl: _yggdrasilServerUrl
                );
            })
            .ToArray();
    }

    #region Factory methods

    // Internal constructor for factory methods
    DefaultYggdrasilAuthenticator(string method, string serverUrl, string clientToken)
    {
        _method = method;
        _yggdrasilServerUrl = serverUrl;
        _clientToken = clientToken;
    }

    /// <summary>
    /// Create a <see cref="DefaultYggdrasilAuthenticator"/> for login
    /// </summary>
    /// <param name="email">Yggdrasil account email</param>
    /// <param name="password">Yggdrasil account password</param>
    /// <param name="yggdrasilServerUrl">Yggdrasil server URL</param>
    /// <param name="clientToken">Yggdrasil client token</param>
    /// <returns><see cref="DefaultYggdrasilAuthenticator"/> for login</returns>
    public static DefaultYggdrasilAuthenticator CreateForLogin(
        string email,
        string password,
        string yggdrasilServerUrl,
        string? clientToken = null
    ) =>
        new("authenticate", yggdrasilServerUrl, clientToken ?? Guid.NewGuid().ToString("N"))
        {
            _email = email,
            _password = password,
        };

    /// <summary>
    /// Create a <see cref="DefaultYggdrasilAuthenticator"/> for refreshing
    /// </summary>
    /// <param name="account"><see cref="Account"/> to be refreshed</param>
    /// <param name="clientToken">Yggdrasil client token</param>
    /// <returns><see cref="DefaultYggdrasilAuthenticator"/> for refreshing</returns>
    public static DefaultYggdrasilAuthenticator CreateForRefresh(
        YggdrasilAccount account,
        string? clientToken = null
    ) =>
        new("refresh", account.YggdrasilServerUrl, clientToken ?? Guid.NewGuid().ToString("N"))
        {
            _accessToken = account.AccessToken,
        };

    #endregion
}
