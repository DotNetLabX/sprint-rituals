using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.API.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/login", async (HttpContext httpContext, string? invite) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/auth/callback"
            };

            if (!string.IsNullOrEmpty(invite))
                properties.Items["invite_token"] = invite;

            await httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
        }).AllowAnonymous();
    }
}
