using Blocks.AspNetCore.Grpc;
using Blocks.Contracts.Grpc;
using Identity.API.Auth;
using Identity.API.Features.Auth.Login;
using Identity.API.Grpc;
using Identity.Persistence;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;

namespace Identity.API;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddIdentityAuth(configuration);
        services.AddIdentityPersistence(configuration);

        // gRPC server (code-first) — Identity hosts PersonGrpcService.
        // AddCodeFirstGrpc is idempotent; calling it in both Fokus and Identity modules is intentional.
        services.AddCodeFirstGrpc();

        // gRPC client — Identity dials Fokus's IInvitationService from the auth callback.
        var grpcOptions = configuration.GetSection("Grpc").Get<GrpcServicesOptions>() ?? new GrpcServicesOptions();
        services.AddCodeFirstGrpcClient<IInvitationService>(grpcOptions, "Invitation");

        return services;
    }

    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        // Login minimal API — Google OAuth challenge (not a FastEndpoints endpoint)
        LoginEndpoint.MapLoginEndpoint(app);
        return app;
    }

    public static WebApplication MapIdentityGrpcServices(this WebApplication app)
    {
        app.MapGrpcService<PersonGrpcService>();
        return app;
    }
}
