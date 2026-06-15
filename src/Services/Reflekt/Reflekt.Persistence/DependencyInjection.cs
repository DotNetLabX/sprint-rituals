using Blocks.Domain.Events;
using Blocks.EntityFrameworkCore.Interceptors;
using Blocks.FastEndpoints;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reflekt.Persistence.Repositories;

namespace Reflekt.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddReflektPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ReflektDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("ReflektDatabase"));
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<TeamRepository>();
        services.AddScoped<RetroSessionRepository>();
        services.AddScoped<ParticipantRepository>();
        services.AddScoped<CardRepository>();
        services.AddScoped<VoteRepository>();
        services.AddScoped<CardGroupRepository>();
        services.AddScoped<FokusSprintSnapshotRepository>();

        return services;
    }

    /// <summary>
    /// Seeds Reflekt development data (phases, sample sprint, participants, cards).
    /// Call after app.Migrate&lt;ReflektDbContext&gt;() in the Host Program.cs Init section.
    /// Only runs when Seed:Enabled == true in configuration.
    /// </summary>
    public static async Task SeedReflektDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ReflektDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        if (config.GetValue<bool>("Seed:Enabled"))
            await DbInitializer.SeedAsync(db);
    }
}

