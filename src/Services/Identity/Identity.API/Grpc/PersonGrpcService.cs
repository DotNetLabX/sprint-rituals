using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Identity.Domain.Enums;
using Identity.Persistence.Repositories;

namespace Identity.API.Grpc;

/// <summary>
/// gRPC server implementation for suite-wide Person identity management.
/// Identity = server. Fokus = client (calls from the Jira-developer sync path).
/// Jira-sourced Persons are matched/created by ExternalId (Jira AccountId) + Source=Jira.
/// Google-sourced Persons are matched by email.
/// </summary>
public class PersonGrpcService(PersonRepository personRepository) : IPersonService
{
    public async Task<UpsertPersonResponse> UpsertPersonAsync(UpsertPersonRequest request, CancellationToken ct = default)
    {
        var person = await FindExistingPersonAsync(request, ct);

        if (person is null)
        {
            person = CreateNewPerson(request);
            await personRepository.AddAsync(person, ct);
            await personRepository.SaveChangesAsync(ct);
            person.RaiseCreated();
            await personRepository.SaveChangesAsync(ct);
        }
        else
        {
            UpdateExistingPerson(person, request);
            await personRepository.SaveChangesAsync(ct);
        }

        return new UpsertPersonResponse { PersonId = person.Id };
    }

    public async Task<FindPersonByEmailResponse> FindPersonByEmailAsync(FindPersonByEmailRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant();
        var person = await personRepository.GetByEmailAsync(email, ct);

        if (person is null)
            return new FindPersonByEmailResponse { Found = false };

        return new FindPersonByEmailResponse
        {
            Found = true,
            PersonId = person.Id,
            Email = person.Email,
            FirstName = person.FirstName,
            LastName = person.LastName,
        };
    }

    private async Task<Identity.Domain.Persons.Person?> FindExistingPersonAsync(UpsertPersonRequest request, CancellationToken ct)
    {
        // Jira-sourced: match by ExternalId + Source (email may be null).
        // Google-sourced: match by email.
        if (request.Source == PersonSource.Jira.ToString() && request.ExternalId is not null)
            return await personRepository.GetByExternalIdAndSourceAsync(request.ExternalId, request.Source, ct);

        if (request.Email is not null)
            return await personRepository.GetByEmailAsync(request.Email, ct);

        return null;
    }

    private static Identity.Domain.Persons.Person CreateNewPerson(UpsertPersonRequest request)
    {
        if (request.Source == PersonSource.Jira.ToString())
            return Identity.Domain.Persons.Person.CreateFromJira(
                request.ExternalId ?? string.Empty,
                request.FirstName,
                request.LastName,
                request.Email,
                request.PictureUrl);

        // Fallback for other sources — use Google factory pattern (email required).
        return Identity.Domain.Persons.Person.CreateFromGoogle(
            request.ExternalId ?? string.Empty,
            request.FirstName,
            request.LastName,
            request.Email ?? string.Empty,
            request.PictureUrl);
    }

    private static void UpdateExistingPerson(Identity.Domain.Persons.Person person, UpsertPersonRequest request)
    {
        if (request.Source == PersonSource.Jira.ToString())
        {
            person.UpdateFromJira(request.FirstName, request.LastName, request.Email, request.PictureUrl);
        }
        else
        {
            person.UpdateFromGoogle(request.FirstName, request.LastName, request.PictureUrl);
        }
    }
}
