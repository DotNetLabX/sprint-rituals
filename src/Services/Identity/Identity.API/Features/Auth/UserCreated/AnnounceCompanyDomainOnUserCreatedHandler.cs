using Blocks.Contracts.IntegrationEvents;
using FastEndpoints;
using Identity.Domain.Users.Events;
using Identity.Persistence.Repositories;
using MassTransit;

namespace Identity.API.Features.Auth.UserCreated;

/// <summary>
/// Publishes <see cref="FirstAdministratorRegistered"/> when the very first Admin user is created.
/// Predicate: Role=="Admin" AND active-admin-count==1 (bootstrap path only).
/// Derives the company domain via email.Split('@')[1]; skips known public providers.
/// Raise MUST occur after SaveChangesAsync so CountActiveAdminsAsync returns 1 (L3).
/// </summary>
public class AnnounceCompanyDomainOnUserCreatedHandler(
    UserRepository userRepository,
    PersonRepository personRepository,
    IPublishEndpoint publishEndpoint)
    : IEventHandler<Identity.Domain.Users.Events.UserCreated>
{
    /// <summary>
    /// Known public email providers — company-domain seed is skipped for these.
    /// Non-exhaustive: only the most common providers are listed.
    /// </summary>
    private static readonly HashSet<string> PublicProviders =
    [
        "gmail.com",
        "googlemail.com",
        "outlook.com",
        "hotmail.com",
        "live.com",
        "yahoo.com",
        "icloud.com",
        "me.com",
        "mac.com",
    ];

    public async Task HandleAsync(Identity.Domain.Users.Events.UserCreated @event, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(@event.UserId, ct);
        if (user is null || user.Role != "Admin")
            return;

        // Gate: only the very first active admin triggers the seed.
        var activeAdminCount = await userRepository.CountActiveAdminsAsync(ct);
        if (activeAdminCount != 1)
            return;

        var person = await personRepository.GetByIdAsync(user.PersonId, ct);
        if (person?.Email is null)
            return;

        var domain = person.Email.Split('@') is [_, var d] ? d : null;
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (PublicProviders.Contains(domain.ToLowerInvariant()))
            return;

        await publishEndpoint.Publish(new FirstAdministratorRegistered(domain), ct);
    }
}
