using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.GetSprintSummary;

public sealed record GetSprintSummaryRequest(int SprintId);

[AllowAnonymous]
[HttpGet("/api/sprints/{sprintId}/summary")]
[Tags("Sprints")]
public sealed class GetSprintSummaryEndpoint(RetroSessionRepository retroSessionRepository, ParticipantRepository participantRepository, TeamRepository teamRepository)
    : Endpoint<GetSprintSummaryRequest, RetroSessionSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest request, CancellationToken ct)
    {
        var sprint = await retroSessionRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        var participantDtos = participants.Select(p => new RetroSessionSummaryParticipant(p.DisplayName)).ToList();

        // Owner comparison keys on the Identity Person id (Team.OwnerId is a Person id after RA2),
        // read from the person_id cookie claim â€” NOT NameIdentifier (the User id), which would
        // compare Person-id â†” User-id and silently set isScrumMaster=false for the real owner.
        var isScrumMaster = false;
        var personIdClaim = User.FindFirstValue("person_id");
        if (personIdClaim is not null && int.TryParse(personIdClaim, out var personId))
        {
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            isScrumMaster = team is not null && team.OwnerId == personId;
        }

        await Send.OkAsync(new RetroSessionSummaryResponse(
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


