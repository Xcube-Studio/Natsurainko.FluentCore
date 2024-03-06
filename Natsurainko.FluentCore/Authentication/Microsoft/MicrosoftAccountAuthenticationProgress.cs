namespace Nrk.FluentCore.Authentication.Microsoft;

public enum MicrosoftAccountAuthenticationProgress
{
    AuthenticatingMicrosoftAccount = 1,
    AuthenticatingWithXboxLive = 2,
    AuthenticatingWithXsts = 3,
    AuthenticatingMinecraftAccount = 4,
    CheckingGameOwnership = 5,
    GettingMinecraftProfile = 6,
    Finish = 7
}