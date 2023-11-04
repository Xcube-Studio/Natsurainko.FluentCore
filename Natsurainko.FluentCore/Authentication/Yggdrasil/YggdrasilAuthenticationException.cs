using System;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class YggdrasilAuthenticationException : Exception
{
    public YggdrasilAuthenticationException(string responseJson)
        : base($"Yggdrasil authentication failed with response\n{responseJson}") { }
}
