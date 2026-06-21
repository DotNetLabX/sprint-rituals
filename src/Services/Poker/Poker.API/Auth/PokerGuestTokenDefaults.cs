namespace Poker.API.Auth;

/// <summary>
/// Poker-local guest-token scheme name. Distinct from Reflekt's "GuestToken" scheme
/// to prevent scheme-name collision at Host boot (Rule 23 / ADR-016).
/// </summary>
public static class PokerGuestTokenDefaults
{
    public const string AuthenticationScheme = "PokerGuestToken";
}
