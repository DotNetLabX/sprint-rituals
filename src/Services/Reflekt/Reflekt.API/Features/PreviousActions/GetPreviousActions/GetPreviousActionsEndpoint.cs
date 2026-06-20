using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.PreviousActions.GetPreviousActions;

public sealed record GetPreviousActionsRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/retro-sessions/{retroSessionId}/previous-actions")]
[Tags("PreviousActions")]
public sealed class GetPreviousActionsEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository)
    : Endpoint<GetPreviousActionsRequest, List<PreviousActionItemResponse>>
{
    public override async Task HandleAsync(GetPreviousActionsRequest req, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(User.FindFirstValue("retroSessionId"), out var claimRetroSessionId) || claimRetroSessionId != req.RetroSessionId)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var userId = User.GetPersonId();
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var previousSession = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(session.TeamId, ct);
        if (previousSession is null)
        {
            await Send.OkAsync([], ct);
            return;
        }

        var unreviewedItems = await cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousSession.Id, ct);

        var response = unreviewedItems
            .Select(c => new PreviousActionItemResponse(
                c.Id,
                c.Text,
                c.AssigneeName,
                c.ReviewStatus.ToString(),
                previousSession.Id,
                previousSession.Name))
            .ToList();

        await Send.OkAsync(response, ct);
    }
}


