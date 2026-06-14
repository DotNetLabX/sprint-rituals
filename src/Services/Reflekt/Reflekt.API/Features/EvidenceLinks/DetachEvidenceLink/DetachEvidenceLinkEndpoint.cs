using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed record DetachEvidenceLinkRequest(int SprintId, int CardId, int EvidenceLinkId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId}")]
[Tags("EvidenceLinks")]
public sealed class DetachEvidenceLinkEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<DetachEvidenceLinkRequest, EmptyResponse>
{
    public override async Task HandleAsync(DetachEvidenceLinkRequest req, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        if (!card.EvidenceLinks.Any(e => e.Id == req.EvidenceLinkId)) { await Send.NotFoundAsync(ct); return; }

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.DetachEvidenceLink))
            ThrowError($"{SprintAction.DetachEvidenceLink} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

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
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
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
