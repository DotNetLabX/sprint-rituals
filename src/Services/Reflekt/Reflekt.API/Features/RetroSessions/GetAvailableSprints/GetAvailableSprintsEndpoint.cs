using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Grpc.Core;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.RetroSessions.GetAvailableSprints;

/// <summary>
/// Lists the Fokus sprints a new retro can be linked to (Active + Closed, newest first).
/// Fokus tracks a single Jira project, so the catalog is not filtered by team.
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams/{teamId}/available-sprints")]
[Tags("RetroSessions")]
public sealed class GetAvailableSprintsEndpoint(TeamRepository teamRepository, ISprintCatalogService sprintCatalogService)
    : Endpoint<GetAvailableSprintsQuery, List<AvailableSprintResponse>>
{
    public override async Task HandleAsync(GetAvailableSprintsQuery query, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdAsync(query.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != User.GetPersonId())
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        SprintCatalogResponse catalog;
        try
        {
            catalog = await sprintCatalogService.GetAvailableSprintsAsync(new TeamQuery(), ct);
        }
        catch (RpcException)
        {
            ThrowError("Fokus is unreachable, so sprints cannot be loaded right now.", StatusCodes.Status503ServiceUnavailable);
            return;
        }

        var sprints = catalog.Sprints
            .Select(s => new AvailableSprintResponse(s.SprintId, s.Name, s.StartDate, s.EndDate, s.State))
            .ToList();

        await Send.OkAsync(sprints, ct);
    }
}
