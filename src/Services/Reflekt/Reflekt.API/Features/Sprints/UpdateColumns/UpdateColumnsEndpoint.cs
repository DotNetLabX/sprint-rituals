using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.UpdateColumns;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPut("/api/sprints/{sprintId}/columns")]
[Tags("Sprints")]
public sealed class UpdateColumnsEndpoint(TeamRepository teamRepository, RetroSessionRepository retroSessionRepository)
    : Endpoint<UpdateColumnsCommand, RetroSessionResponse>
{
    public override async Task HandleAsync(UpdateColumnsCommand command, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var sprint = await retroSessionRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        sprint.UpdateColumnConfig(
            command.Column1Name, command.Column1Color,
            command.Column2Name, command.Column2Color,
            command.Column3Name, command.Column3Color);

        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.OkAsync(sprint.Adapt<RetroSessionResponse>(), ct);
    }
}


