using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.ListCards;

public sealed record ListCardsRequest(int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class ListCardsEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : Endpoint<ListCardsRequest, List<CardResponse>>
{
    public override async Task HandleAsync(ListCardsRequest req, CancellationToken ct)
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

        var cards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        var groupIds = cards.Where(c => c.GroupId.HasValue).Select(c => c.GroupId!.Value).Distinct();
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groupIds, ct);

        var requestingParticipantId = participantIdClaim is not null ? int.Parse(participantIdClaim) : (int?)null;
        var redactOthers = sprint.CurrentPhase == RetroPhase.Writing && requestingParticipantId is not null;

        await Send.OkAsync(
            cards.Select(c =>
            {
                var voteCount = c.GroupId.HasValue ? voteCounts.GetValueOrDefault(c.GroupId.Value, 0) : 0;
                var response = c.Adapt<CardResponse>() with { VoteCount = voteCount };
                if (redactOthers && c.ParticipantId != requestingParticipantId)
                    return response with { Text = "", AssigneeName = null, EvidenceLinks = [] };
                return response;
            }).ToList(),
            ct);
    }
}


