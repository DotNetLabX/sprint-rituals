using Fokus.API.Features.Analytics;
using Fokus.API.Features.Settings;
using Fokus.API.Features.Sync;
using Fokus.API.Features.Xray;
using Fokus.API.Hubs;
using Fokus.Persistence;
using Jira.RestApi;
using Xray.GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFokusPersistence(configuration);

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

    public static WebApplication UseFokusMiddleware(this WebApplication app)
    {
        app.MapHub<SprintHub>("/hubs/sprint");
        return app;
    }
}
