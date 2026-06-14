using Microsoft.AspNetCore.Authentication;

namespace Reflekt.API.Auth;

/// <summary>
/// Reflekt authentication registration.
/// RA1: Identity owns AddAuthentication/AddCookie/AddGoogle (single suite scheme).
/// This extension adds ONLY the Reflekt guest-token scheme onto the existing auth builder.
/// OnGoogleTicketReceived (writes Reflekt User) is removed — RA2 work (ADR-016).
/// </summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddReflektAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Chain the guest scheme onto Identity's already-registered authentication builder.
        // Identity's AddIdentityAuth has called AddAuthentication(Cookie default) + AddCookie + AddGoogle.
        // We add only the guest scheme here so Reflekt's guest-gated endpoints authenticate via guest tokens.
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>(
                GuestTokenDefaults.AuthenticationScheme, _ => { });

        return services;
    }
}
