using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication;

[JsonSerializable(typeof(XBLAuthenticateRequest))]
[JsonSerializable(typeof(XBLAuthenticateResponse))]
[JsonSerializable(typeof(XSTSAuthenticateRequest))]
[JsonSerializable(typeof(XSTSAuthenticateErrorModel))]
[JsonSerializable(typeof(XSTSAuthenticateResponse))]
[JsonSerializable(typeof(MicrosoftAuthenticationResponse))]
[JsonSerializable(typeof(OAuth2DeviceCodeResponse))]
[JsonSerializable(typeof(OAuth2TokenResponse))]
[JsonSerializable(typeof(YggdrasilLoginRequest))]
[JsonSerializable(typeof(YggdrasilRefreshRequest))]
[JsonSerializable(typeof(YggdrasilResponseModel))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class AuthenticationJsonSerializerContext : JsonSerializerContext { }