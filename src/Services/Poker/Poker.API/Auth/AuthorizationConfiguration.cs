using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Poker.API.Auth;

/// <summary>
/// Poker authorization policies. SM = authenticated Identity cookie. Voter = Poker guest token.
/// </summary>
public static class AuthorizationConfiguration
{
    public static IServiceCollection AddPokerAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.ScrumMaster, policy =>
                policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser())
            .AddPolicy(Policies.Voter, policy =>
                policy.AddAuthenticationSchemes(PokerGuestTokenDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser())
            .AddPolicy(Policies.ScrumMasterOrVoter, policy =>
                policy.AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PokerGuestTokenDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser());

        return services;
    }
}
