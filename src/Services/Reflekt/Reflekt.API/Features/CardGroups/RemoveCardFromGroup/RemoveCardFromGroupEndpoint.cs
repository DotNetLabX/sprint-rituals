using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.RemoveCardFromGroup;

public sealed record RemoveCardFromGroupRequest(int SprintId, int GroupId, int CardId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId}")]
[Tags("CardGroups")]
public sealed class RemoveCardFromGroupEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<RemoveCardFromGroupRequest>
{
    public override async Task HandleAsync(RemoveCardFromGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.UngroupCards))
            ThrowError($"{SprintAction.UngroupCards} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.GroupId != req.GroupId) { await Send.NotFoundAsync(ct); return; }

        var soloGroup = CardGroup.Create(req.SprintId, card.ColumnType);
        soloGroup.ClearDomainEvents();
        await cardGroupRepository.AddAsync(soloGroup, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        card.SetGroupId(soloGroup.Id);
        group.RecordCardRemoved(req.CardId, soloGroup.Id);
        await cardGroupRepository.SaveChangesAsync(ct);

        await Send.OkAsync(ct);
    }
}
