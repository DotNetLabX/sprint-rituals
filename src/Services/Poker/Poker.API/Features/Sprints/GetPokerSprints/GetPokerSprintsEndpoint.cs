using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Sprints.GetPokerSprints;

/// <summary>
/// GET /api/poker/sprints — List Active + Future + recent Closed sprints for the SM sprint selector.
/// Default is first future sprint (Rule 16). 401 if unauthenticated.
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/poker/sprints")]
[Tags("PokerSprints")]
public sealed class GetPokerSprintsEndpoint(IPokerFokusService pokerFokusService)
    : EndpointWithoutRequest<GetPokerSprintsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await pokerFokusService.GetPokerSprintsAsync(new PokerSprintsQuery { RecentClosedCount = 3 }, ct);

        // Rule 16: default selection is first future sprint (sorted by start date).
        var defaultSprintId = result.Sprints
            .Where(s => s.State == "future")
            .OrderBy(s => s.StartDate)
            .FirstOrDefault()?.SprintId;

        await Send.OkAsync(new GetPokerSprintsResponse(result.Sprints, defaultSprintId), ct);
    }
}

public record GetPokerSprintsResponse(
    IReadOnlyList<PokerSprintInfo> Sprints,
    int? DefaultSprintId);
