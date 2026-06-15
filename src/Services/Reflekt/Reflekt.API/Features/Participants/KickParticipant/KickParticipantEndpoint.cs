using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.KickParticipant;

public sealed record KickParticipantRequest(int SprintId, int ParticipantId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/participants/{participantId}")]
[Tags("Participants")]
public sealed class KickParticipantEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    ParticipantRepository participantRepository)
    : Endpoint<KickParticipantRequest, EmptyResponse>
{
    public override async Task HandleAsync(KickParticipantRequest req, CancellationToken ct)
    {
        var sprint = await retroSessionRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != RetroSessionState.Active)
            ThrowError("Cannot kick participants from a non-active sprint.", StatusCodes.Status409Conflict);

        var userId = User.GetPersonId();
        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var participant = await participantRepository.GetByIdAsync(req.ParticipantId, ct);
        if (participant is null || participant.SprintId != req.SprintId)
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


