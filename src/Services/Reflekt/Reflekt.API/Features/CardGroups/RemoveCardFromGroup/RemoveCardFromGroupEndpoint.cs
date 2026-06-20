using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.RemoveCardFromGroup;

public sealed record RemoveCardFromGroupRequest(int RetroSessionId, int GroupId, int CardId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}/cards/{cardId}")]
[Tags("CardGroups")]
public sealed class RemoveCardFromGroupEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<RemoveCardFromGroupRequest>
{
    public override async Task HandleAsync(RemoveCardFromGroupRequest req, CancellationToken ct)
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

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.UngroupCards))
            ThrowError($"{RetroAction.UngroupCards} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.GroupId != req.GroupId) { await Send.NotFoundAsync(ct); return; }

        var soloGroup = CardGroup.Create(req.RetroSessionId, card.ColumnType);
        soloGroup.ClearDomainEvents();
        await cardGroupRepository.AddAsync(soloGroup, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        card.SetGroupId(soloGroup.Id);
        group.RecordCardRemoved(req.CardId, soloGroup.Id);
        await cardGroupRepository.SaveChangesAsync(ct);

        await Send.OkAsync(ct);
    }
}


