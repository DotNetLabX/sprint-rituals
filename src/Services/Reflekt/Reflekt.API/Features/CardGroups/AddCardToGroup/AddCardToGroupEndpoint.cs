using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups.Events;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.AddCardToGroup;

public sealed record AddCardToGroupRequest(int SprintId, int GroupId, int CardId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/cards")]
[Tags("CardGroups")]
public sealed class AddCardToGroupEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<AddCardToGroupRequest, CardGroupResponse>
{
    public override async Task HandleAsync(AddCardToGroupRequest req, CancellationToken ct)
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

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, RetroAction.GroupCards))
            ThrowError($"{RetroAction.GroupCards} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        if (card.CardType != CardType.Note)
            ThrowError("Only Note cards can be grouped.", StatusCodes.Status400BadRequest);

        if (card.ColumnType != group.ColumnType)
            ThrowError("Card must be in the same column as the group.", StatusCodes.Status400BadRequest);

        var sourceGroupId = card.GroupId ?? 0;
        card.SetGroupId(group.Id);
        group.RecordCardAdded(req.CardId, sourceGroupId);

        if (sourceGroupId != 0 && sourceGroupId != group.Id)
        {
            var allCards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
            if (!allCards.Any(c => c.GroupId == sourceGroupId))
            {
                var sourceGroup = await cardGroupRepository.GetByIdAsync(sourceGroupId, ct);
                if (sourceGroup is not null)
                    cardGroupRepository.Remove(sourceGroup);
            }
        }

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


