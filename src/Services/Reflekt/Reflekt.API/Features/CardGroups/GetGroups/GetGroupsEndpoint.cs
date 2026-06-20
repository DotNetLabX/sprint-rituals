using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.GetGroups;

public sealed record GetGroupsRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/retro-sessions/{retroSessionId}/card-groups")]
[Tags("CardGroups")]
public sealed class GetGroupsEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository)
    : Endpoint<GetGroupsRequest, List<CardGroupResponse>>
{
    public override async Task HandleAsync(GetGroupsRequest req, CancellationToken ct)
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

        var groups = await cardGroupRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var cards = await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var noteCards = cards.Where(c => c.CardType == CardType.Note && c.GroupId.HasValue);
        var cardsByGroup = noteCards.GroupBy(c => c.GroupId!.Value).ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

        var voteCounts = session.CurrentPhase >= RetroPhase.Voting
            ? await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct)
            : new Dictionary<int, int>();

        await Send.OkAsync(
            groups.Select(g => g.Adapt<CardGroupResponse>() with
            {
                Cards = cardsByGroup.GetValueOrDefault(g.Id, []).Select(c => c.Adapt<CardGroupCardEntry>()).ToList(),
                VoteCount = voteCounts.GetValueOrDefault(g.Id, 0),
                IsFocused = session.FocusedGroupId == g.Id
            }).ToList(),
            ct);
    }
}


