using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class XBLAuthenticateRequest
{
    public class XBLAuthenticateProperties
    {
        [JsonPropertyName("AuthMethod")]
        public string AuthMethod { get; set; } = "RPS";

        [JsonPropertyName("SiteName")]
        public string SiteName { get; set; } = "user.auth.xboxlive.com";

        [JsonPropertyName("RpsTicket")]
        public string RpsTicket { get; set; } = "d=<access token>";
    }

    [JsonPropertyName("Properties")]
    public XBLAuthenticateProperties Properties { get; set; } = new XBLAuthenticateProperties();

    [JsonPropertyName("RelyingParty")]
    public string RelyingParty { get; set; } = "http://auth.xboxlive.com";

    [JsonPropertyName("TokenType")]
    public string TokenType { get; set; } = "JWT";
}

public class XSTSAuthenticateRequest
{
    public class XSTSAuthenticateProperties
    {
        [JsonPropertyName("SandboxId")]
        public string SandboxId { get; set; } = "RETAIL";

        [JsonPropertyName("UserTokens")]
        public List<string> UserTokens { get; set; } = new List<string>();
    }

    [JsonPropertyName("Properties")]
    public XSTSAuthenticateProperties Properties { get; set; } = new XSTSAuthenticateProperties();

    [JsonPropertyName("RelyingParty")]
    public string RelyingParty { get; set; } = "rp://api.minecraftservices.com/";

    [JsonPropertyName("TokenType")]
    public string TokenType { get; set; } = "JWT";
}
