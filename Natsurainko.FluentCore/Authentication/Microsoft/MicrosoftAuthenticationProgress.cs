namespace Nrk.FluentCore.Authentication;

public enum MicrosoftAuthenticationProgress
{
    AuthenticatingMicrosoftAccount = 1,
    AuthenticatingWithXboxLive = 2,
    AuthenticatingWithXsts = 3,
    AuthenticatingMinecraftAccount = 4,
    CheckingGameOwnership = 5,
    GettingMinecraftProfile = 6,
    Finish = 7
}