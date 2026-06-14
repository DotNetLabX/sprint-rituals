using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Reflekt.API.Auth;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams.CreateTeam;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams")]
[Tags("Teams")]
public sealed class CreateTeamEndpoint(
    TeamRepository teamRepository,
    IOptions<TeamSettings> teamSettings)
    : Endpoint<CreateTeamCommand, CreateTeamResponse>
{
    public override async Task HandleAsync(CreateTeamCommand command, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var limit = teamSettings.Value.MaxTeamsPerUser;

        var count = await teamRepository.CountByOwnerIdAsync(userId, ct);
        if (count >= limit)
        {
            AddError("limit_reached: Team limit reached. Delete a team to create a new one.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (await teamRepository.ExistsByOwnerAndNameAsync(userId, command.Name, ct))
        {
            AddError($"duplicate_name: You already have a team called '{command.Name}'.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var team = Team.Create(command.Name, userId);
        await teamRepository.AddAsync(team, ct);
        await teamRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(new CreateTeamResponse(team.Id, team.Name, team.CreatedAt), 201, ct);
    }
}
