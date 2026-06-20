using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.RetroSessions.GetTeamRetros;

public sealed record GetTeamRetrosRequest(int TeamId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams/{teamId}/retro-sessions")]
[Tags("RetroSessions")]
public sealed class GetTeamRetrosEndpoint(TeamRepository teamRepository, RetroSessionRepository retroSessionRepository)
    : Endpoint<GetTeamRetrosRequest, List<RetroSessionResponse>>
{
    public override async Task HandleAsync(GetTeamRetrosRequest request, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var sessions = await retroSessionRepository.GetByTeamIdAsync(request.TeamId, ct);
        await Send.OkAsync(sessions.Select(s => s.Adapt<RetroSessionResponse>()).ToList(), ct);
    }
}


