using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams.GetMyTeams;

public sealed record TeamSummary(
    int Id,
    string Name,
    DateTime CreatedAt,
    int SprintCount,
    DateTime? LastSprintDate,
    bool HasActiveSprint);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams")]
[Tags("Teams")]
public sealed class GetMyTeamsEndpoint(TeamRepository teamRepository)
    : EndpointWithoutRequest<List<TeamSummary>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var summaries = await teamRepository.GetTeamSummariesByOwnerAsync(userId, ct);
        var result = summaries
            .Select(s => new TeamSummary(s.Id, s.Name, s.CreatedAt, s.SprintCount, s.LastSprintDate, s.HasActiveSprint))
            .ToList();
        await Send.OkAsync(result, ct);
    }
}
