using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.JoinSprint;

[AllowAnonymous]
[HttpPost("/api/sprints/{sprintId}/join")]
[Tags("Participants")]
public sealed class JoinSprintEndpoint(SprintRepository sprintRepository, ParticipantRepository participantRepository)
    : Endpoint<JoinSprintCommand, JoinSprintResponse>
{
    public override async Task HandleAsync(JoinSprintCommand command, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != SprintState.Active)
            ThrowError("Sprint is not accepting participants.", StatusCodes.Status409Conflict);

        if (await participantRepository.ExistsByDisplayNameAsync(command.SprintId, command.DisplayName, ct))
            ThrowError("Name already taken.", StatusCodes.Status409Conflict);

        var participant = Participant.Join(command.DisplayName, command.SprintId);
        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(participant.Adapt<JoinSprintResponse>(), 201, ct);
    }
}
