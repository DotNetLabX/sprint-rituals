using Blocks.AspNetCore.Grpc;
using Blocks.Contracts.Grpc;
using Poker.API.Auth;
using Poker.Persistence;

namespace Poker.API;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Poker services: persistence, Mapster, auth, gRPC client.
    /// Called from Host/Program.cs Add section.
    /// </summary>
    public static IServiceCollection AddPokerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPokerPersistence(configuration);

        // Mapster
        Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(Mapster.TypeAdapterConfig.GlobalSettings);
        services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();

        // Auth (Poker guest scheme + authorization policies)
        services.AddPokerAuthentication(configuration);
        services.AddPokerAuthorization();

        // gRPC client — Poker dials Fokus's IPokerFokusService for sprint/ticket queries and write-back.
        // Config key: Grpc:Services:PokerFokus:Url
        var grpcOptions = configuration.GetSection("Grpc").Get<GrpcServicesOptions>() ?? new GrpcServicesOptions();
        services.AddCodeFirstGrpcClient<IPokerFokusService>(grpcOptions, "PokerFokus");

        return services;
    }

    /// <summary>
    /// Maps Poker hub and endpoints. Called from Host/Program.cs Use section.
    /// Hub at /hubs/poker (distinct from /hubs/retro and /hubs/sprint).
    /// </summary>
    public static WebApplication MapPokerEndpoints(this WebApplication app)
    {
        app.MapHub<Poker.API.Hubs.PlanningHub>("/hubs/poker");
        return app;
    }
}
