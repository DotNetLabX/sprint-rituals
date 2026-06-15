using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.GetTeamSprints;

public sealed record GetTeamSprintsRequest(int TeamId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams/{teamId}/retro-sessions")]
[Tags("Sprints")]
public sealed class GetTeamSprintsEndpoint(TeamRepository teamRepository, RetroSessionRepository retroSessionRepository)
    : Endpoint<GetTeamSprintsRequest, List<RetroSessionResponse>>
{
    public override async Task HandleAsync(GetTeamSprintsRequest request, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var sprints = await retroSessionRepository.GetByTeamIdAsync(request.TeamId, ct);
        await Send.OkAsync(sprints.Select(s => s.Adapt<RetroSessionResponse>()).ToList(), ct);
    }
}


