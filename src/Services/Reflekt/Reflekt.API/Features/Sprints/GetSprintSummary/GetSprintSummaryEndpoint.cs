using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.GetSprintSummary;

public sealed record GetSprintSummaryRequest(int SprintId);

[AllowAnonymous]
[HttpGet("/api/sprints/{sprintId}/summary")]
[Tags("Sprints")]
public sealed class GetSprintSummaryEndpoint(SprintRepository sprintRepository, ParticipantRepository participantRepository, TeamRepository teamRepository)
    : Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        var participantDtos = participants.Select(p => new SprintSummaryParticipant(p.DisplayName)).ToList();

        var isScrumMaster = false;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && int.TryParse(userIdClaim, out var userId))
        {
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            isScrumMaster = team is not null && team.OwnerId == userId;
        }

        await Send.OkAsync(new SprintSummaryResponse(
            sprint.Id,
            sprint.TeamId,
            sprint.Name,
            sprint.State.ToString(),
            sprint.CurrentPhase.ToString(),
            sprint.VotesPerParticipant,
            isScrumMaster,
            participantDtos,
            sprint.Column1Name,
            sprint.Column1Color.Value,
            sprint.Column2Name,
            sprint.Column2Color.Value,
            sprint.Column3Name,
            sprint.Column3Color.Value,
            sprint.WritingTimerDefault,
            sprint.VotingTimerDefault,
            sprint.DiscussingTimerDefault), ct);
    }
}
