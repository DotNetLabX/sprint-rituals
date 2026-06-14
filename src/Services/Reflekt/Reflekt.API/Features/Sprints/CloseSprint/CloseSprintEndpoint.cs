using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.CloseSprint;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/close")]
[Tags("Sprints")]
public sealed class CloseSprintEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null || sprint.TeamId != request.TeamId) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != SprintState.Active)
        {
            AddError("Sprint must be Active to close.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        sprint.Close();
        await sprintRepository.SaveChangesAsync(ct);

        await Send.OkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
