using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed record ClearFocusRequest(int RetroSessionId);

public sealed record ClearFocusResponse(int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/focus")]
[Tags("CardGroups")]
public sealed class ClearFocusEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<ClearFocusRequest, ClearFocusResponse>
{
    public override async Task HandleAsync(ClearFocusRequest req, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (session.State != RetroSessionState.Active)
        {
            AddError("Retro must be Active.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.FocusGroup))
            ThrowError($"{RetroAction.FocusGroup} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        int? previousGroupId = session.FocusedGroupId;
        bool? previousGroupDiscussed = null;

        if (previousGroupId.HasValue)
        {
            var previousGroup = await cardGroupRepository.GetByIdAsync(previousGroupId.Value, ct);
            if (previousGroup is not null)
            {
                previousGroup.MarkDiscussed();
                previousGroupDiscussed = true;
            }
        }

        session.ClearFocus();
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new ClearFocusResponse(previousGroupId, previousGroupDiscussed), ct);
    }
}


