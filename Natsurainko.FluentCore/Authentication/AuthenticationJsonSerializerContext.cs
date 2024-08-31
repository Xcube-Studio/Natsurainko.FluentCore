using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
internal partial class AuthenticationJsonSerializerContext : JsonSerializerContext
{
}