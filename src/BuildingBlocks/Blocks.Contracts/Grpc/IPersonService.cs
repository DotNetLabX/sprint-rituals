using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

/// <summary>
/// gRPC contract for suite-wide Person identity management.
/// Identity = server. Fokus = client (calls from the Jira-developer sync path).
/// Jira-sourced Persons are keyed on ExternalId (Jira AccountId) + Source, NOT email
/// (Jira Cloud does not expose developer email by default).
/// Google-sourced Persons are keyed on email.
/// </summary>
[ServiceContract]
public interface IPersonService
{
    /// <summary>
    /// Upserts a Person into Identity. For Jira source, matches by ExternalId+Source;
    /// for Google source, matches by email. Returns the PersonId.
    /// </summary>
    [OperationContract]
    Task<UpsertPersonResponse> UpsertPersonAsync(UpsertPersonRequest request, CancellationToken ct = default);

    /// <summary>
    /// Finds a Person by email. Defined for the full contract surface; unconsumed this pass.
    /// </summary>
    [OperationContract]
    Task<FindPersonByEmailResponse> FindPersonByEmailAsync(FindPersonByEmailRequest request, CancellationToken ct = default);
}
