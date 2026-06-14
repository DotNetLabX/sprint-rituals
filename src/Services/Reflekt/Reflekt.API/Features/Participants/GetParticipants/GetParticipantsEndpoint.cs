using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.GetParticipants;

public sealed record GetParticipantsRequest(int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/sprints/{sprintId}/participants")]
[Tags("Participants")]
public sealed class GetParticipantsEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetParticipantsRequest, List<ParticipantResponse>>
{
    public override async Task HandleAsync(GetParticipantsRequest request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var isGuestToken = User.FindFirstValue("participantId") is not null;

        if (isGuestToken)
        {
            if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != request.SprintId)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var userId = User.GetUserId();
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        await Send.OkAsync(participants.Select(p => p.Adapt<ParticipantResponse>()).ToList(), ct);
    }
}
