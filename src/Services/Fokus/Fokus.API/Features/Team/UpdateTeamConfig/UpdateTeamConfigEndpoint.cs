namespace Fokus.API.Features.Team.UpdateTeamConfig;

[HttpPut("/api/developers/{accountId}/team-config")]
[Tags("Team")]
[Authorize(Roles = "Admin")]
public class UpdateTeamConfigEndpoint(DeveloperRepository developerRepository)
    : Endpoint<UpdateTeamConfigCommand, UpdateTeamConfigResponse>
{
    public override async Task HandleAsync(UpdateTeamConfigCommand req, CancellationToken ct)
    {
        var developer = await developerRepository.UpdateTeamConfigAsync(
            req.AccountId,
            req.Role,
            req.DefaultCapacityPercent,
            req.SubTeam,
            req.SubTeamProvided ?? false,
            req.IsActive,
            ct);

        if (developer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await developerRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new UpdateTeamConfigResponse
        {
            AccountId = developer.Id,
            DisplayName = developer.DisplayName,
            AvatarUrl = developer.AvatarUrl,
            Role = developer.Role,
            DefaultCapacityPercent = developer.DefaultCapacityPercent,
            SubTeam = developer.SubTeam,
            IsActive = developer.IsActive
        }, ct);
    }
}
