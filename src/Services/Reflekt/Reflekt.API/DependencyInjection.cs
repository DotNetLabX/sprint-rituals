
using Blocks.AspNetCore.Grpc;
using Blocks.Contracts.Grpc;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Features.RetroSessions.Timer;
using Reflekt.API.Features.Teams;
using Reflekt.API.Hubs;
using Reflekt.API.OpenGraph;
using Reflekt.Persistence;

namespace Reflekt.API;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Reflekt services: persistence, Mapster, auth, OpenGraph, caches, timers.
    /// Called from Host/Program.cs Add section.
    /// </summary>
    public static IServiceCollection AddReflektModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReflektPersistence(configuration);

        // gRPC client — Reflekt dials Fokus's ISprintAnalyticsService for the sprint-context panel.
        // This is Reflekt's first gRPC client. Config key: Grpc:Services:SprintAnalytics:Url.
        // Mirror Fokus.API/DependencyInjection.cs:42-43 (ADR-019).
        var grpcOptions = configuration.GetSection("Grpc").Get<GrpcServicesOptions>() ?? new GrpcServicesOptions();
        services.AddCodeFirstGrpcClient<ISprintAnalyticsService>(grpcOptions, "SprintAnalytics");
        services.AddCodeFirstGrpcClient<ISprintCatalogService>(grpcOptions, "SprintCatalog");

        // Mapster
        Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(Mapster.TypeAdapterConfig.GlobalSettings);
        services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();

        // Options
        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.Configure<TeamSettings>(configuration.GetSection("TeamSettings"));

        // Auth (guest scheme added in AddReflektAuthentication; authorization policies)
        services.AddReflektAuthentication(configuration);
        services.AddReflektAuthorization();

        // Feature services
        services.AddReflektOpenGraph();
        services.AddSingleton<PhaseActionCache>();
        services.AddSingleton<TimerService>();
        services.AddScoped<Features.RetroSessions.GetSprintContext.GetSprintContextAssembler>();
        services.AddScoped<Reflekt.Domain.Reports.RetroReportBuilder>();

        return services;
    }

    /// <summary>
    /// Registers Reflekt's MassTransit consumers onto the shared in-memory bus.
    /// Called from Host/Program.cs inside the single AddMassTransit block (ADR-019, Step 4).
    /// </summary>
    public static void AddReflektMessaging(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumers(typeof(DependencyInjection).Assembly);
    }

    /// <summary>
    /// Maps Reflekt endpoints and hub. Hub at /hubs/retro (F2: /hubs/sprint taken by Fokus).
    /// Timer expiry callback wired here.
    /// Called from Host/Program.cs Use section.
    /// </summary>
    public static WebApplication MapReflektEndpoints(this WebApplication app)
    {
        // Hub route: /hubs/retro (NOT /hubs/sprint — taken by Fokus.SprintHub per F2)
        // Frontend URL updated in RA5.
        app.MapHub<RetroHub>("/hubs/retro");

        // Wire timer expiry callback: on expiry, broadcast TimerUpdated(expired) to retro group
        var timerService = app.Services.GetRequiredService<TimerService>();
        var hubContext = app.Services.GetRequiredService<IHubContext<RetroHub>>();
        timerService.RegisterExpiryCallback(async retroSessionId =>
        {
            await hubContext.Clients.Group($"retro:{retroSessionId}").SendAsync("TimerUpdated", new
            {
                status = "expired",
                remainingSeconds = 0,
                endTimeUtc = (DateTime?)null,
            });
        });

        return app;
    }
}
