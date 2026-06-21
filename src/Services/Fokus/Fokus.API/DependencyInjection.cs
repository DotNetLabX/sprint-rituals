using Blocks.AspNetCore.Grpc;
using Blocks.Contracts.Grpc;
using Fokus.API.Features.Analytics;
using Fokus.API.Features.Settings;
using Fokus.API.Features.Sync;
using Fokus.API.Features.Xray;
using Fokus.API.Grpc;
using Fokus.API.Hubs;
using Fokus.Domain.Analytics;
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

        // MassTransit consumers are registered in Host/Program.cs AddMassTransit (single shared bus).
        // Fokus consumers are discovered via AddFokusMessaging(cfg) called from Host.

        services.AddSignalR();
        services.AddRestApiJira(configuration);
        services.AddGraphQLXray(configuration);

        // gRPC client — Fokus dials Identity's IPersonService from the Jira-developer sync path.
        var grpcOptions = configuration.GetSection("Grpc").Get<GrpcServicesOptions>() ?? new GrpcServicesOptions();
        services.AddCodeFirstGrpcClient<IPersonService>(grpcOptions, "Person");

        services.AddScoped<SprintIssueSyncService>();
        services.AddScoped<XrayIssueSyncService>();
        services.AddScoped<WorkflowDetectionService>();
        services.AddScoped<SprintSummaryAnalyzer>();
        services.AddScoped<QaMetricsAnalyzer>();
        services.AddScoped<TestTimelineAnalyzer>();
        services.AddScoped<DeveloperThroughputAnalyzer>();
        services.AddScoped<ScopeChangeAnalyzer>();
        services.AddScoped<CarryOverAnalyzer>();
        services.AddScoped<BugRatioAnalyzer>();
        services.AddScoped<EpicProgressAnalyzer>();
        services.AddScoped<CycleTimeAnalyzer>();
        services.AddScoped<LeaderboardCalculator>();
        services.AddScoped<DeveloperQualityScorer>();
        services.AddScoped<QaWorkloadAnalyzer>();
        services.AddScoped<QaTrendsAnalyzer>();
        services.AddScoped<DeveloperProgressAnalyzer>();
        services.AddScoped<DeveloperDetailAnalyzer>();

        return services;
    }

    /// <summary>
    /// Registers Fokus's MassTransit consumers onto the shared in-memory bus.
    /// Called from Host/Program.cs inside the single AddMassTransit block (Step 4 topology).
    /// </summary>
    public static void AddFokusMessaging(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumers(typeof(DependencyInjection).Assembly);
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
            settings.Xray.Enabled = true;
            settings.Xray.ClientId = xrayOptions.ClientId;
            settings.Xray.ClientSecret = xrayOptions.ClientSecret;
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
        app.MapGrpcService<InvitationGrpcService>();
        app.MapGrpcService<PokerFokusGrpcService>();
        return app;
    }
}
