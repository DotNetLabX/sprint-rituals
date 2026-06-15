using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.JoinSprint;

[AllowAnonymous]
[HttpPost("/api/sprints/{sprintId}/join")]
[Tags("Participants")]
public sealed class JoinSprintEndpoint(RetroSessionRepository retroSessionRepository, ParticipantRepository participantRepository)
    : Endpoint<JoinSprintCommand, JoinSprintResponse>
{
    public override async Task HandleAsync(JoinSprintCommand command, CancellationToken ct)
    {
        var sprint = await retroSessionRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != RetroSessionState.Active)
            ThrowError("Sprint is not accepting participants.", StatusCodes.Status409Conflict);

        if (await participantRepository.ExistsByDisplayNameAsync(command.SprintId, command.DisplayName, ct))
            ThrowError("Name already taken.", StatusCodes.Status409Conflict);

        // Optional cookie claim: suite members (Identity cookie) carry person_id → PersonId set;
        // guests have no cookie → null (ADR-022). Endpoint stays [AllowAnonymous] either way.
        // Seam (ADR-021): a future SM-approval gate slots in before this single Join call.
        var personId = User.GetPersonIdOrNull();
        var participant = Participant.Join(command.DisplayName, command.SprintId, personId);
        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(participant.Adapt<JoinSprintResponse>(), 201, ct);
    }
}


