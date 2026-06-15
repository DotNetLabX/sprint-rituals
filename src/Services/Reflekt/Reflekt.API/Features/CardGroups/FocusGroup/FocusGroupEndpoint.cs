using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed record FocusGroupRequest(int SprintId, int GroupId);

public sealed record FocusGroupResponse(int FocusedGroupId, int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/focus")]
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

        var targetGroup = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (targetGroup is null || targetGroup.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        int? previousGroupId = sprint.FocusedGroupId;
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

        sprint.SetFocus(req.GroupId);
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new FocusGroupResponse(req.GroupId, previousGroupId, previousGroupDiscussed), ct);
    }
}


