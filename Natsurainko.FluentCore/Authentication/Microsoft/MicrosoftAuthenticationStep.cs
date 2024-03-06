namespace Nrk.FluentCore.Authentication.Microsoft;

public enum MicrosoftAuthenticationStep
{
    AuthenticateMicrosoftAccount = 1,
    AuthenticateXboxLive = 2,
    AuthenticateXsts = 3,
    AuthenticateMinecraftAccount = 4,
    CheckGameOwnership = 5,
    GetMinecraftProfile = 6,
    Finish = 7
}