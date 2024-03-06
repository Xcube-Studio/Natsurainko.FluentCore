using System;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class MicrosoftAccountAuthenticationException : Exception
{
    public MicrosoftAccountAuthenticationExceptionType Type { get; internal set; }

    public MicrosoftAccountAuthenticationProgress Step { get; internal set; }

    public MicrosoftAccountAuthenticationException(string message)
        : base(message) { }
}

public enum MicrosoftAccountAuthenticationExceptionType
{
    Unknown = 0,
    NetworkConnectionError = 1,
    XboxLiveError = 3,
    GameOwnershipError = 4,
}
