using Blocks.Domain.Events;
using Blocks.EntityFrameworkCore.Interceptors;
using Blocks.FastEndpoints;
using Fokus.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fokus.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<FokusDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("Database"));
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<SprintRepository>();
        services.AddScoped<TicketRepository>();
        services.AddScoped<DeveloperRepository>();
        services.AddScoped<AppSettingsRepository>();
        services.AddScoped<InvitationRepository>();
        services.AddScoped<TestExecutionRepository>();

        return services;
    }
}
