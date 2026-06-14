
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Features.Sprints.Timer;
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

        return services;
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
        app.MapHub<SprintHub>("/hubs/retro");

        // Wire timer expiry callback: on expiry, broadcast TimerUpdated(expired) to sprint group
        var timerService = app.Services.GetRequiredService<TimerService>();
        var hubContext = app.Services.GetRequiredService<IHubContext<SprintHub>>();
        timerService.RegisterExpiryCallback(async sprintId =>
        {
            await hubContext.Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", new
            {
                status = "expired",
                remainingSeconds = 0,
                endTimeUtc = (DateTime?)null,
            });
        });

        return app;
    }
}
