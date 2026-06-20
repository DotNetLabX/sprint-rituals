using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.KickParticipant;

public sealed record KickParticipantRequest(int RetroSessionId, int ParticipantId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/participants/{participantId}")]
[Tags("Participants")]
public sealed class KickParticipantEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    ParticipantRepository participantRepository)
    : Endpoint<KickParticipantRequest, EmptyResponse>
{
    public override async Task HandleAsync(KickParticipantRequest req, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State != RetroSessionState.Active)
            ThrowError("Cannot kick participants from a non-active session.", StatusCodes.Status409Conflict);

        var userId = User.GetPersonId();
        var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var participant = await participantRepository.GetByIdAsync(req.ParticipantId, ct);
        if (participant is null || participant.RetroSessionId != req.RetroSessionId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        participant.Kick();
        participantRepository.Remove(participant);
        await participantRepository.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}


