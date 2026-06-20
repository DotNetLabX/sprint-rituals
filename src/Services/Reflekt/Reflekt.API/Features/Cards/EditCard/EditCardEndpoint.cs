using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.EditCard;

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpPut("/api/retro-sessions/{retroSessionId}/cards/{cardId}")]
[Tags("Cards")]
public sealed class EditCardEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<EditCardCommand, CardResponse>
{
    public override async Task HandleAsync(EditCardCommand command, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.RetroSessionId != command.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        var session = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

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
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            var personId = User.GetPersonId();
            if (team is null || team.OwnerId != personId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var action = card.CardType == CardType.ActionItem ? RetroAction.EditActionItem : RetroAction.EditNote;
        if (!phaseActionCache.IsAllowed(session.CurrentPhase, action))
            ThrowError($"{action} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (session.CurrentPhase == RetroPhase.Discussing && card.CardType == CardType.ActionItem)
        {
            // Presence check: only a cookie-authenticated suite user may edit an action item in
            // Discussing. Any Identity cookie carries person_id (RA2), so this is behavior-equivalent
            // to the former NameIdentifier check.
            if (User.FindFirstValue("person_id") is null)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        card.Update(command.Text, command.AssigneeName);

        await cardRepository.SaveChangesAsync(ct);
        await Send.OkAsync(card.Adapt<CardResponse>(), ct);
    }
}


