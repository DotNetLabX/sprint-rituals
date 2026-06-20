using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed record FocusGroupRequest(int RetroSessionId, int GroupId);

public sealed record FocusGroupResponse(int FocusedGroupId, int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}/focus")]
[Tags("CardGroups")]
public sealed class FocusGroupEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<FocusGroupRequest, FocusGroupResponse>
{
    public override async Task HandleAsync(FocusGroupRequest req, CancellationToken ct)
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

        var targetGroup = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (targetGroup is null || targetGroup.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        int? previousGroupId = session.FocusedGroupId;
        bool? previousGroupDiscussed = null;

        if (previousGroupId.HasValue && previousGroupId.Value != req.GroupId)
        {
            var previousGroup = await cardGroupRepository.GetByIdAsync(previousGroupId.Value, ct);
            if (previousGroup is not null)
            {
                previousGroup.MarkDiscussed();
                previousGroupDiscussed = true;
            }
        }

        session.SetFocus(req.GroupId);
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new FocusGroupResponse(req.GroupId, previousGroupId, previousGroupDiscussed), ct);
    }
}


