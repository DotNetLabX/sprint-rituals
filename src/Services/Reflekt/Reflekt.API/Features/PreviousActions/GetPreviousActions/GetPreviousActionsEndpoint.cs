using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.PreviousActions.GetPreviousActions;

public sealed record GetPreviousActionsRequest(int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/sprints/{sprintId}/previous-actions")]
[Tags("PreviousActions")]
public sealed class GetPreviousActionsEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository)
    : Endpoint<GetPreviousActionsRequest, List<PreviousActionItemResponse>>
{
    public override async Task HandleAsync(GetPreviousActionsRequest req, CancellationToken ct)
    {
        var sprint = await retroSessionRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != req.SprintId)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var userId = User.GetPersonId();
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var previousSprint = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(sprint.TeamId, ct);
        if (previousSprint is null)
        {
            await Send.OkAsync([], ct);
            return;
        }

        var unreviewedItems = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);

        var response = unreviewedItems
            .Select(c => new PreviousActionItemResponse(
                c.Id,
                c.Text,
                c.AssigneeName,
                c.ReviewStatus.ToString(),
                previousSprint.Id,
                previousSprint.Name))
            .ToList();

        await Send.OkAsync(response, ct);
    }
}


