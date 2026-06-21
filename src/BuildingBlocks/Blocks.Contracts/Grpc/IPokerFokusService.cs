using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

/// <summary>
/// Fokus gRPC contract for Poker-specific queries.
/// Fokus is the server; Poker.API is the client.
/// Distinct from ISprintCatalogService — do not extend it.
/// </summary>
[ServiceContract]
public interface IPokerFokusService
{
    /// <summary>
    /// Returns Active + Future sprints plus the N most-recently Closed sprints.
    /// Sibling of GetAnalyticsSprintsAsync — does NOT share its Closed|Active filter.
    /// </summary>
    [OperationContract]
    Task<PokerSprintsResponse> GetPokerSprintsAsync(PokerSprintsQuery request, CancellationToken ct = default);

    /// <summary>
    /// Live pass-through to Jira for a sprint's tickets. No local caching.
    /// </summary>
    [OperationContract]
    Task<SprintTicketsResponse> GetSprintTicketsAsync(SprintTicketsQuery request, CancellationToken ct = default);

    /// <summary>
    /// Writes the agreed story-point estimate back to Jira via the Fokus Jira client.
    /// Returns Success=false + error message when Jira rejects the update.
    /// </summary>
    [OperationContract]
    Task<SetStoryPointsResponse> SetStoryPointsAsync(SetStoryPointsRequest request, CancellationToken ct = default);
}
