using Nrk.FluentCore.Authentication.Microsoft;
using Nrk.FluentCore.Utils;
using System;
using System.Linq;
using System.Text.Json;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class DefaultYggdrasilAuthenticator : IAuthenticator<YggdrasilAccount>
{
    private string _yggdrasilServerUrl;
    private string _email;
    private string _password;
    private string _accessToken;
    private string _clientToken = Guid.NewGuid().ToString("N");
    private string _method;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public YggdrasilAccount[] Authenticate()
    {
        string url = _yggdrasilServerUrl;
        string content = string.Empty;

        switch (_method)
        {
            case "authenticate":
                url += "/authserver/authenticate";
                content = JsonSerializer.Serialize(new LoginRequestModel
                {
                    ClientToken = _clientToken,
                    UserName = _email,
                    Password = _password
                });
                break;
            case "refresh":
                url += "/authserver/refresh";
                content = JsonSerializer.Serialize(new
                {
                    ClientToken = _clientToken,
                    accessToken = _accessToken,
                    requestUser = true
                });
                break;
        }

        using var res = HttpUtils.HttpPost(url, content);
        string result = res.Content.ReadAsString();

        res.EnsureSuccessStatusCode();

        var model = JsonSerializer.Deserialize<YggdrasilResponseModel>(result);

        return model.AvailableProfiles.Select(profile => new YggdrasilAccount
        (
            Name: profile.Name,
            Uuid: Guid.Parse(profile.Id),
            AccessToken: model.AccessToken,
            YggdrasilServerUrl: _yggdrasilServerUrl
        )).ToArray();
    }

    public static DefaultYggdrasilAuthenticator CreateForLogin(string email, string password, string yggdrasilServerUrl, string clientToken = null) => new()
    {
        _method = "authenticate",
        _email = email,
        _password = password,
        _yggdrasilServerUrl = yggdrasilServerUrl,
        _clientToken = clientToken ?? Guid.NewGuid().ToString("N")
    };

    public static DefaultYggdrasilAuthenticator CreateForRefresh(YggdrasilAccount account, string clientToken = null) => new()
    {
        _method = "refresh",
        _clientToken = clientToken ?? Guid.NewGuid().ToString("N"),
        _accessToken = account.AccessToken,
        _yggdrasilServerUrl = account.YggdrasilServerUrl
    };
}
