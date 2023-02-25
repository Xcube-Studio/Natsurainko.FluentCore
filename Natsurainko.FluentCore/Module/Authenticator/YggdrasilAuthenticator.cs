using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Authenticator;

public class YggdrasilAuthenticator : IAuthenticator
{
    public string YggdrasilServerUrl { get; private set; }

    public string Email { get; private set; }

    public string Password { get; private set; }

    public string AccessToken { get; private set; }

    public string ClientToken { get; private set; } = Guid.NewGuid().ToString("N");

    public AuthenticatorMethod Method { get; private set; }

    public YggdrasilAuthenticator(
        string yggdrasilServerUrl = "https://authserver.mojang.com",
        AuthenticatorMethod method = AuthenticatorMethod.Login)
    {
        YggdrasilServerUrl = yggdrasilServerUrl;
        Method = method;
    }

    public YggdrasilAuthenticator(
        AuthenticatorMethod method = AuthenticatorMethod.Login,
        string accessToken = default,
        string clientToken = default,
        string email = default,
        string password = default,
        string yggdrasilServerUrl = "https://authserver.mojang.com")
    {
        Email = email;
        Password = password;
        AccessToken = accessToken;
        ClientToken = clientToken;

        YggdrasilServerUrl = yggdrasilServerUrl;
        Method = method;
    }

    public IAccount Authenticate()
        => AuthenticateAsync().GetAwaiter().GetResult();

    public IAccount Authenticate(Func<IEnumerable<ProfileModel>, Task<ProfileModel>> selectProfileFunc)
        => AuthenticateAsync(selectProfileFunc).GetAwaiter().GetResult();

    public async Task<IAccount> AuthenticateAsync() => await AuthenticateAsync(profiles => Task.Run(() => profiles.First()));

    public async Task<IAccount> AuthenticateAsync(Func<IEnumerable<ProfileModel>, Task<ProfileModel>> selectProfileFunc)
    {
        string url = YggdrasilServerUrl;
        string content = string.Empty;

        switch (Method)
        {
            case AuthenticatorMethod.Login:
                url += "/authserver/authenticate";
                content = new LoginRequestModel
                {
                    ClientToken = ClientToken,
                    UserName = Email,
                    Password = Password
                }.ToJson();
                break;
            case AuthenticatorMethod.Refresh:
                url += "/authserver/refresh";
                content = new
                {
                    clientToken = ClientToken,
                    accessToken = AccessToken,
                    requestUser = true
                }.ToJson();
                break;
            default:
                break;
        }

        using var res = await HttpWrapper.HttpPostAsync(url, content);
        string result = await res.Content.ReadAsStringAsync();

        res.EnsureSuccessStatusCode();

        var model = JsonConvert.DeserializeObject<YggdrasilResponseModel>(result);
        model.SelectedProfile ??= await selectProfileFunc(model.AvailableProfiles);

        return new YggdrasilAccount()
        {
            AccessToken = model.AccessToken,
            ClientToken = model.ClientToken,
            Name = model.SelectedProfile.Name,
            Uuid = Guid.Parse(model.SelectedProfile.Id),
            YggdrasilServerUrl = YggdrasilServerUrl
        };
    }

    public async Task<bool> ValidateAsync(string accessToken)
    {
        string content = JsonConvert.SerializeObject(
            new YggdrasilRequestModel
            {
                ClientToken = ClientToken,
                AccessToken = accessToken
            }
        );

        using var res = await HttpWrapper.HttpPostAsync($"{YggdrasilServerUrl}/authserver/validate", content);

        return res.IsSuccessStatusCode;
    }

    public async Task<bool> SignoutAsync()
    {
        string content = JsonConvert.SerializeObject(
            new
            {
                username = Email,
                password = Password
            }
        );

        using var res = await HttpWrapper.HttpPostAsync($"{YggdrasilServerUrl}/authserver/signout", content);

        return res.IsSuccessStatusCode;
    }

    public async Task<bool> InvalidateAsync(string accessToken)
    {
        string content = JsonConvert.SerializeObject(
            new YggdrasilRequestModel
            {
                ClientToken = ClientToken,
                AccessToken = accessToken
            }
        );

        using var res = await HttpWrapper.HttpPostAsync($"{YggdrasilServerUrl}/authserver/invalidate", content);

        return res.IsSuccessStatusCode;
    }
}
