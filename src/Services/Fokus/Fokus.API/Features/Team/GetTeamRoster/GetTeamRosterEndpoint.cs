namespace Fokus.API.Features.Team.GetTeamRoster;

[HttpGet("/api/team")]
[Tags("Team")]
public class GetTeamRosterEndpoint(DeveloperRepository developerRepository)
    : EndpointWithoutRequest<TeamRosterResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var developers = await developerRepository.GetAllAsync(ct);
        var subTeams = await developerRepository.GetDistinctSubTeamsAsync(ct);

        var dtos = developers.Select(d => new TeamDeveloperDto
        {
            AccountId = d.Id,
            DisplayName = d.DisplayName,
            AvatarUrl = d.AvatarUrl,
            Role = d.Role,
            DefaultCapacityPercent = d.DefaultCapacityPercent,
            SubTeam = d.SubTeam,
            IsActive = d.IsActive
        }).ToList();

        await Send.OkAsync(new TeamRosterResponse
        {
            Developers = dtos,
            SubTeams = subTeams
        }, ct);
    }
}
