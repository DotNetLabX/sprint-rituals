using Microsoft.AspNetCore.Authentication;

namespace Poker.API.Auth;

/// <summary>
/// Chains the Poker guest-token scheme onto the shared auth builder (Identity already owns cookie+Google).
/// Called from AddPokerModule → AddPokerAuthentication.
/// </summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddPokerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Chain Poker's guest scheme onto the auth builder Identity already registered.
        // Scheme name "PokerGuestToken" is distinct from Reflekt's "GuestToken" (Rule 23).
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, PokerGuestTokenAuthenticationHandler>(
                PokerGuestTokenDefaults.AuthenticationScheme, _ => { });

        return services;
    }
}
