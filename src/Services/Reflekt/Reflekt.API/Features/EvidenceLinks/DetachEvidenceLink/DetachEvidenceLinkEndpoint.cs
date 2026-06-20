using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed record DetachEvidenceLinkRequest(int RetroSessionId, int CardId, int EvidenceLinkId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/cards/{cardId}/evidence-links/{evidenceLinkId}")]
[Tags("EvidenceLinks")]
public sealed class DetachEvidenceLinkEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<DetachEvidenceLinkRequest, EmptyResponse>
{
    public override async Task HandleAsync(DetachEvidenceLinkRequest req, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        if (!card.EvidenceLinks.Any(e => e.Id == req.EvidenceLinkId)) { await Send.NotFoundAsync(ct); return; }

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.DetachEvidenceLink))
            ThrowError($"{RetroAction.DetachEvidenceLink} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

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
            var userId = User.GetPersonId();
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        card.DetachEvidenceLink(req.EvidenceLinkId);
        await cardRepository.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}


