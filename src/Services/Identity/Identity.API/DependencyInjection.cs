using Identity.API.Auth;
using Identity.API.Features.Auth.Login;
using Identity.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.API;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityAuth(configuration);
        services.AddIdentityPersistence(configuration);
        return services;
    }

    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        // Login minimal API — Google OAuth challenge (not a FastEndpoints endpoint)
        LoginEndpoint.MapLoginEndpoint(app);
        return app;
    }
}
