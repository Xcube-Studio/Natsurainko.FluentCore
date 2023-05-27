using Natsurainko.FluentCore.Model.Auth;
using System;

namespace Natsurainko.FluentCore.Exceptions;

public class MicrosoftAuthenticationException : Exception
{
    public MicrosoftAuthenticationExceptionType Type { get; internal set; }

    public new string HelpLink { get; internal set; }

    public MicrosoftAuthenticationStep Step { get; internal set; }

    public new string Message { get; internal set; }

    public new Exception InnerException { get; internal set; }
}
