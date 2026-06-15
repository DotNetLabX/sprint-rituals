using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed record ClearFocusRequest(int SprintId);

public sealed record ClearFocusResponse(int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/focus")]
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

        var sprint = await retroSessionRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (sprint.State != RetroSessionState.Active)
        {
            AddError("Sprint must be Active.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, RetroAction.FocusGroup))
            ThrowError($"{RetroAction.FocusGroup} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        int? previousGroupId = sprint.FocusedGroupId;
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

        sprint.ClearFocus();
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new ClearFocusResponse(previousGroupId, previousGroupDiscussed), ct);
    }
}


