using Fokus.API.Features.Analytics;
using Fokus.API.Features.Settings;
using Fokus.API.Features.Sync;
using Fokus.API.Features.Xray;
using Fokus.API.Grpc;
using Fokus.API.Hubs;
using Fokus.Persistence;
using Jira.RestApi;
using Mapster;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc.Server;
using Xray.GraphQL;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusModule(this IServiceCollection services, IConfiguration configuration)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddFokusPersistence(configuration);

        // gRPC server (code-first protobuf-net.Grpc)
        services.AddCodeFirstGrpc();

        // MassTransit — in-memory transport (no RabbitMQ; integration events within the monorepo)
        services.AddMassTransit(x => x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context)));

        services.AddSignalR();
        services.AddRestApiJira(configuration);
        services.AddGraphQLXray(configuration);

        services.AddScoped<SprintIssueSyncService>();
        services.AddScoped<XrayIssueSyncService>();
        services.AddScoped<WorkflowDetectionService>();
        services.AddScoped<SprintSummaryService>();
        services.AddScoped<QaMetricsService>();
        services.AddScoped<DeveloperThroughputService>();
        services.AddScoped<ScopeChangeService>();
        services.AddScoped<CarryOverService>();
        services.AddScoped<BugRatioService>();
        services.AddScoped<EpicProgressService>();
        services.AddScoped<CycleTimeService>();
        services.AddScoped<LeaderboardService>();
        services.AddScoped<DeveloperQualityService>();
        services.AddScoped<QaWorkloadService>();
        services.AddScoped<TestTimelineService>();
        services.AddScoped<QaTrendsService>();
        services.AddScoped<DeveloperProgressService>();
        services.AddScoped<DeveloperDetailService>();

        return services;
    }

    /// <summary>
    /// Seeds Fokus startup data: AppSettings default row and Xray config from appsettings.
    /// Call after <c>app.Migrate&lt;FokusDbContext&gt;()</c> in the Program.cs Init section.
    /// </summary>
    public static async Task SeedFokusDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var fokusDb = sp.GetRequiredService<FokusDbContext>();

        // Seed AppSettings default row if missing
        if (!await fokusDb.AppSettings.AnyAsync())
        {
            fokusDb.AppSettings.Add(Fokus.Domain.AppSettings.CreateDefault());
            await fokusDb.SaveChangesAsync();
        }

        // Seed Xray settings from config if present
        var xrayOptions = sp.GetRequiredService<IOptions<XrayOptions>>().Value;
        if (!string.IsNullOrEmpty(xrayOptions.ClientId) && !string.IsNullOrEmpty(xrayOptions.ClientSecret))
        {
            var settings = await fokusDb.AppSettings.FirstOrDefaultAsync();
            if (settings is null) return;   // row was not seeded — skip Xray wiring
            settings.XrayEnabled = true;
            settings.XrayClientId = xrayOptions.ClientId;
            settings.XrayClientSecret = xrayOptions.ClientSecret;
            await fokusDb.SaveChangesAsync();
        }
    }

    public static WebApplication UseFokusMiddleware(this WebApplication app)
    {
        app.MapHub<SprintHub>("/hubs/sprint");
        return app;
    }

    public static WebApplication MapFokusEndpoints(this WebApplication app)
    {
        // gRPC services (h2c endpoint configured in appsettings.json Kestrel:Endpoints:Grpc)
        app.MapGrpcService<SprintAnalyticsGrpcService>();
        app.MapGrpcService<SprintCatalogGrpcService>();
        return app;
    }
}
