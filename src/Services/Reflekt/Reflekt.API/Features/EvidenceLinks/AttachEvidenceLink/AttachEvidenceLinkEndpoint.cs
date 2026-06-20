using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.Cards;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink;

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpPost("/api/retro-sessions/{retroSessionId}/cards/{cardId}/evidence-links")]
[Tags("EvidenceLinks")]
public sealed class AttachEvidenceLinkEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    IConfiguration configuration,
    PhaseActionCache phaseActionCache)
    : Endpoint<AttachEvidenceLinkCommand, EvidenceLinkResponse>
{
    public override async Task HandleAsync(AttachEvidenceLinkCommand command, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.RetroSessionId != command.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        var session = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.AttachEvidenceLink))
            ThrowError($"{RetroAction.AttachEvidenceLink} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

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

        var jiraDomain = configuration["EvidenceLinks:JiraDomain"];
        var (source, externalKey) = EvidenceLinkSourceDetector.Detect(command.Url, jiraDomain);

        var link = card.AttachEvidenceLink(command.Url, source, externalKey);
        await cardRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(link.Adapt<EvidenceLinkResponse>(), 201, ct);
    }
}


