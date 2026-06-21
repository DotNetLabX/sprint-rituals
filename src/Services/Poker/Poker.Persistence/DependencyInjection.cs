using Blocks.Domain.Events;
using Blocks.EntityFrameworkCore.Interceptors;
using Blocks.FastEndpoints;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Poker.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPokerPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<PokerDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("PokerDatabase"));
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<PlanningSessionRepository>();
        services.AddScoped<SessionParticipantRepository>();
        services.AddScoped<EstimationItemRepository>();

        return services;
    }
}
