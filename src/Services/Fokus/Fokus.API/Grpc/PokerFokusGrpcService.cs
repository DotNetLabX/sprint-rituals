using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Fokus.Persistence.Repositories;
using Jira.Contracts;

namespace Fokus.API.Grpc;

/// <summary>
/// gRPC server for Poker-specific Fokus surfaces: sprint list, live ticket pass-through, and story-point write-back.
/// Fokus = server; Poker.API = client.
/// Distinct from ISprintCatalogService — Poker needs Active+Future+recent Closed (not the analytics closed window).
/// </summary>
public class PokerFokusGrpcService(SprintRepository sprintRepository, IJiraClient jiraClient) : IPokerFokusService
{
    public async Task<PokerSprintsResponse> GetPokerSprintsAsync(PokerSprintsQuery request, CancellationToken ct = default)
    {
        var recentClosedCount = request.RecentClosedCount > 0 ? request.RecentClosedCount : 3;
        var sprints = await sprintRepository.GetPokerSprintsAsync(recentClosedCount, ct);

        var sprintInfos = sprints
            .Select(s => new PokerSprintInfo
            {
                SprintId = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                State = s.State.ToString().ToLowerInvariant(),
            })
            .ToList();

        return new PokerSprintsResponse { Sprints = sprintInfos };
    }

    public async Task<SprintTicketsResponse> GetSprintTicketsAsync(SprintTicketsQuery request, CancellationToken ct = default)
    {
        // Live pass-through — no local caching; callers should use sparingly
        var issues = await jiraClient.GetSprintIssuesAsync(request.SprintId, ct);

        var tickets = issues
            .Select(i => new PokerTicket
            {
                Key = i.Key,
                Summary = i.Fields.Summary,
                StoryPoints = i.Fields.StoryPoints,
            })
            .ToList();

        return new SprintTicketsResponse { Tickets = tickets };
    }

    public async Task<SetStoryPointsResponse> SetStoryPointsAsync(SetStoryPointsRequest request, CancellationToken ct = default)
    {
        try
        {
            await jiraClient.SetStoryPointsAsync(request.IssueKey, request.StoryPoints, ct);
            return new SetStoryPointsResponse { Success = true };
        }
        catch (Exception ex)
        {
            return new SetStoryPointsResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}
