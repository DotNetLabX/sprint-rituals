using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.UpdateGroupTitle;

public sealed record UpdateGroupTitleRequest(int SprintId, int GroupId, string? Title);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPatch("/api/sprints/{sprintId}/card-groups/{groupId}")]
[Tags("CardGroups")]
public sealed class UpdateGroupTitleEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<UpdateGroupTitleRequest, CardGroupResponse>
{
    public override async Task HandleAsync(UpdateGroupTitleRequest req, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.RenameGroup))
            ThrowError($"{SprintAction.RenameGroup} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        group.SetTitle(req.Title);

        await cardGroupRepository.SaveChangesAsync(ct);

        var groupCards = (await cardRepository.GetBySprintIdAsync(req.SprintId, ct))
            .Where(c => c.GroupId == group.Id)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        await Send.OkAsync(group.Adapt<CardGroupResponse>() with
        {
            Cards = groupCards.Select(c => c.Adapt<CardGroupCardEntry>()).ToList(),
            IsFocused = sprint.FocusedGroupId == group.Id
        }, ct);
    }
}
