using System;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class MicrosoftAuthenticationException : Exception
{
    public MicrosoftAuthenticateExceptionType Type { get; internal set; }

    public MicrosoftAuthenticateStep Step { get; internal set; }

    public MicrosoftAuthenticationException(string message)
        : base(message) { }
}

public enum MicrosoftAuthenticateExceptionType
{
    Unknown = 0,
    NetworkConnectionError = 1,
    XboxLiveError = 3,
    GameOwnershipError = 4,
}
