using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.ToggleDiscussed;

public sealed record ToggleDiscussedRequest(int SprintId, int GroupId);

public sealed record ToggleDiscussedResponse(int GroupId, bool IsDiscussed, DateTime? DiscussedAt);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/toggle-discussed")]
[Tags("CardGroups")]
public sealed class ToggleDiscussedEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<ToggleDiscussedRequest, ToggleDiscussedResponse>
{
    public override async Task HandleAsync(ToggleDiscussedRequest req, CancellationToken ct)
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

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, RetroAction.ToggleDiscussed))
            ThrowError($"{RetroAction.ToggleDiscussed} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        if (group.DiscussedAt.HasValue)
            group.UnmarkDiscussed();
        else
            group.MarkDiscussed();

        await cardGroupRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new ToggleDiscussedResponse(group.Id, group.DiscussedAt.HasValue, group.DiscussedAt), ct);
    }
}


