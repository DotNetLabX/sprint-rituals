using Microsoft.AspNetCore.Authentication.Cookies;

namespace Reflekt.API.Auth;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddReflektAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.ScrumMaster, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.Participant, policy => policy
                .AddAuthenticationSchemes(GuestTokenDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.OwnerOrScrumMaster, policy => policy
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    GuestTokenDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.GoogleUser, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());

        return services;
    }
}
