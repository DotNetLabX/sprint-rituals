using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.RetroSessions.StartRetro;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/retro-sessions/{retroSessionId}/start")]
[Tags("RetroSessions")]
public sealed class StartRetroEndpoint(TeamRepository teamRepository, RetroSessionRepository retroSessionRepository)
    : Endpoint<RetroSessionRouteRequest, RetroSessionResponse>
{
    public override async Task HandleAsync(RetroSessionRouteRequest request, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var session = await retroSessionRepository.GetByIdAsync(request.RetroSessionId, ct);
        if (session is null || session.TeamId != request.TeamId) { await Send.NotFoundAsync(ct); return; }

        if (session.State != RetroSessionState.Draft)
        {
            AddError("Retro must be in Draft state to start.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        session.Start();
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(session.Adapt<RetroSessionResponse>(), ct);
    }
}


