using Blocks.Domain.Events;
using Blocks.EntityFrameworkCore.Interceptors;
using Blocks.FastEndpoints;
using Identity.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("IdentityDatabase"));
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<PersonRepository>();
        services.AddScoped<UserRepository>();

        return services;
    }
}
