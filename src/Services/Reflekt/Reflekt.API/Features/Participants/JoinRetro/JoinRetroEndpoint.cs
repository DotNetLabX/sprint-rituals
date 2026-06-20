using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.JoinRetro;

[AllowAnonymous]
[HttpPost("/api/retro-sessions/{retroSessionId}/join")]
[Tags("Participants")]
public sealed class JoinRetroEndpoint(RetroSessionRepository retroSessionRepository, ParticipantRepository participantRepository)
    : Endpoint<JoinRetroCommand, JoinRetroResponse>
{
    public override async Task HandleAsync(JoinRetroCommand command, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State != RetroSessionState.Active)
            ThrowError("Retro is not accepting participants.", StatusCodes.Status409Conflict);

        if (await participantRepository.ExistsByDisplayNameAsync(command.RetroSessionId, command.DisplayName, ct))
            ThrowError("Name already taken.", StatusCodes.Status409Conflict);

        // Optional cookie claim: suite members (Identity cookie) carry person_id → PersonId set;
        // guests have no cookie → null (ADR-022). Endpoint stays [AllowAnonymous] either way.
        // Seam (ADR-021): a future SM-approval gate slots in before this single Join call.
        var personId = User.GetPersonIdOrNull();
        var participant = Participant.Join(command.DisplayName, command.RetroSessionId, personId);
        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(participant.Adapt<JoinRetroResponse>(), 201, ct);
    }
}


