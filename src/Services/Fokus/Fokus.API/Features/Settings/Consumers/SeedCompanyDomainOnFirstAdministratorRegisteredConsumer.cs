using Blocks.Contracts.IntegrationEvents;
using Fokus.Persistence.Repositories;
using MassTransit;

namespace Fokus.API.Features.Settings.Consumers;

/// <summary>
/// Seeds <see cref="AppSettings.CompanyDomain"/> when the first administrator registers.
/// Idempotent: only sets the domain if it is currently null (never overwrites an existing value).
/// Mutates the loaded AppSettings instance via the repository — never constructs a new AppSettings
/// object directly, to avoid wiping ComplexProperty value-object columns (L5).
/// </summary>
public sealed class SeedCompanyDomainOnFirstAdministratorRegisteredConsumer(
    AppSettingsRepository appSettingsRepository)
    : IConsumer<FirstAdministratorRegistered>
{
    public async Task Consume(ConsumeContext<FirstAdministratorRegistered> context)
    {
        var settings = await appSettingsRepository.GetAsync(context.CancellationToken);

        if (settings.CompanyDomain is not null)
            return;

        settings.CompanyDomain = context.Message.CompanyDomain;
        await appSettingsRepository.SaveAsync(settings, context.CancellationToken);
    }
}
