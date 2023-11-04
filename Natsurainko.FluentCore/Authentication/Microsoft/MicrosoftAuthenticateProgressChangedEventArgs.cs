namespace Nrk.FluentCore.Authentication.Microsoft;

public class MicrosoftAuthenticateProgressChangedEventArgs
{
    public MicrosoftAuthenticateStep AuthenticateStep { get; set; }

    public double Progress { get; set; }

    public static implicit operator MicrosoftAuthenticateProgressChangedEventArgs((MicrosoftAuthenticateStep, double) value) => new()
    {
        AuthenticateStep = value.Item1,
        Progress = value.Item2
    };
}
