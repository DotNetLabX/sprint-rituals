using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.DeleteCard;

public sealed record DeleteCardRequest(int SprintId, int CardId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}")]
[Tags("Cards")]
public sealed class DeleteCardEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<DeleteCardRequest, EmptyResponse>
{
    public override async Task HandleAsync(DeleteCardRequest req, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(participantIdClaim, out var claimParticipantId) || claimParticipantId != card.ParticipantId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }
        else
        {
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            var userId = User.GetUserId();
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var action = card.CardType == CardType.ActionItem ? SprintAction.DeleteActionItem : SprintAction.DeleteNote;
        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, action))
            ThrowError($"{action} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (sprint.CurrentPhase == SprintPhase.Discussing && card.CardType == CardType.ActionItem)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is null)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        card.MarkDeleted();
        cardRepository.Remove(card);
        await cardRepository.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
