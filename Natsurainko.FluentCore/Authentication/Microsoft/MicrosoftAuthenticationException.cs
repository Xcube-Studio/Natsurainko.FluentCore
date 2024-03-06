using System;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class MicrosoftAuthenticationException : Exception
{
    public MicrosoftAuthenticationExceptionType Type { get; internal set; }

    public MicrosoftAuthenticationStep Step { get; internal set; }

    public MicrosoftAuthenticationException(string message)
        : base(message) { }
}

public enum MicrosoftAuthenticationExceptionType
{
    Unknown = 0,
    NetworkConnectionError = 1,
    XboxLiveError = 3,
    GameOwnershipError = 4,
}
