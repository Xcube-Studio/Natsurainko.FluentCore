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

public enum MicrosoftAuthenticateStep
{
    Get_Authorization_Token = 1,
    Authenticate_with_XboxLive = 2,
    Obtain_XSTS_token_for_Minecraft = 3,
    Authenticate_with_Minecraft = 4,
    Checking_Game_Ownership = 5,
    Get_the_profile = 6,
    Finished = 7
}