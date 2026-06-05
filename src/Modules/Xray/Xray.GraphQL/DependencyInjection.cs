using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xray.Contracts;

namespace Xray.GraphQL;

public static class DependencyInjection
{
    public static IServiceCollection AddGraphQLXray(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<XrayOptions>()
            .Bind(configuration.GetSection("Xray"));
        // Named HttpClient for auth endpoint (used by XrayBearerTokenManager)
        services.AddHttpClient("XrayAuth");

        // Typed HttpClient for GraphQL queries
        services.AddHttpClient<GraphQLXrayClient>(client =>
        {
            client.BaseAddress = new Uri("https://xray.cloud.getxray.app");
        });

        services.AddSingleton<XrayBearerTokenManager>();
        services.AddSingleton<XrayRateLimiter>();
        services.AddScoped<IXrayClient>(sp => sp.GetRequiredService<GraphQLXrayClient>());

        return services;
    }
}
