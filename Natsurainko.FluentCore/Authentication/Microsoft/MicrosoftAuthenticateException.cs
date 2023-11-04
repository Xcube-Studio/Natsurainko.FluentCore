using System;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class MicrosoftAuthenticateException : Exception
{
    public MicrosoftAuthenticateException(string message) : base(message)
    {

    }

    public MicrosoftAuthenticateExceptionType Type { get; internal set; }

    public MicrosoftAuthenticateStep Step { get; internal set; }
}
