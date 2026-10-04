using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Identity.API.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/login", async (HttpContext httpContext, IConfiguration configuration, string? invite, string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/auth/callback"
            };

            if (!string.IsNullOrEmpty(invite))
                properties.Items["invite_token"] = invite;

            // Round-trip the caller's landing page through the OAuth state so each SPA returns to where
            // it started (its own origin in dev — e.g. the Vite server on :5172) rather than a fixed module.
            var allowedOrigins = configuration.GetSection("Auth:AllowedReturnOrigins").Get<string[]>() ?? [];
            var safeReturnUrl = SanitizeReturnUrl(returnUrl, allowedOrigins);
            if (safeReturnUrl is not null)
                properties.Items["returnUrl"] = safeReturnUrl;

            await httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
        }).AllowAnonymous();
    }

    /// <summary>
    /// Open-redirect guard for the post-login return target. Accepts a same-origin relative path
    /// (rejecting protocol-relative "//" and backslash tricks) or an absolute URL whose origin is
    /// explicitly allowlisted. Anything else returns null and the caller falls back to the default.
    /// </summary>
    private static string? SanitizeReturnUrl(string? returnUrl, string[] allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
            return returnUrl;

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var origin = $"{uri.Scheme}://{uri.Authority}";
            if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return returnUrl;
        }

        return null;
    }
}
