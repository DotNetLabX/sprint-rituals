using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Reports.GetRetroReport;

public sealed record GetRetroReportRequest(int TeamId, int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/teams/{teamId}/retro-sessions/{retroSessionId}/report")]
[Tags("Reports")]
public sealed class GetRetroReportEndpoint(
    TeamRepository teamRepository,
    RetroSessionRepository retroSessionRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetRetroReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetRetroReportRequest req, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null || session.TeamId != req.TeamId) { await Send.NotFoundAsync(ct); return; }

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
            var team = await teamRepository.GetByIdAsync(req.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var cards = await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var groups = await cardGroupRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var teamInfo = await teamRepository.GetByIdAsync(req.TeamId, ct);

        var cardsByGroup = cards
            .Where(c => c.CardType == CardType.Note && c.GroupId.HasValue)
            .GroupBy(c => c.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

        var groupEntries = groups
            .Select(g =>
            {
                var votes = voteCounts.GetValueOrDefault(g.Id, 0);
                var groupCards = cardsByGroup.GetValueOrDefault(g.Id, []);
                var title = g.Title ?? groupCards.FirstOrDefault()?.Text ?? "";
                var isDiscussed = g.DiscussedAt.HasValue;
                return (Group: g, Title: title, Votes: votes, Cards: groupCards, IsDiscussed: isDiscussed);
            })
            .Where(x => x.IsDiscussed || x.Votes > 1)
            .OrderByDescending(x => x.Votes)
            .ToList();

        var columnSections = new List<ReportColumnSection>
        {
            BuildColumnSection(session.Column1.Name, session.Column1.Color.Value, ColumnType.Column1, groupEntries),
            BuildColumnSection(session.Column2.Name, session.Column2.Color.Value, ColumnType.Column2, groupEntries),
            BuildColumnSection(session.Column3.Name, session.Column3.Color.Value, ColumnType.Column3, groupEntries),
        };

        var actionItems = cards
            .Where(c => c.CardType == CardType.ActionItem)
            .Select(c => c.Adapt<ReportActionItemEntry>())
            .ToList();

        var previousActionsSection = await BuildPreviousActionsSectionAsync(req.RetroSessionId, session.TeamId, ct);

        var response = new ReportResponse(
            new ReportRetroSessionInfo(session.Id, session.Name, session.CreatedAt, session.ClosedAt),
            new ReportTeamInfo(teamInfo!.Id, teamInfo.Name),
            participants.Select(p => new ReportParticipantInfo(p.DisplayName)).ToList(),
            columnSections,
            actionItems,
            previousActionsSection);

        await Send.OkAsync(response, ct);
    }

    private static ReportColumnSection BuildColumnSection(
        string columnName,
        string columnColor,
        ColumnType columnType,
        List<(Domain.CardGroups.CardGroup Group, string Title, int Votes, List<Domain.Cards.Card> Cards, bool IsDiscussed)> groupEntries)
    {
        var groups = groupEntries
            .Where(x => x.Group.ColumnType == columnType)
            .Select(x => new ReportGroupEntry(x.Title, x.Votes, x.IsDiscussed, x.Cards.Select(c => c.Adapt<ReportCardEntry>()).ToList()))
            .ToList();
        return new ReportColumnSection(columnName, columnColor, groups);
    }

    private async Task<ReportPreviousActionsSection?> BuildPreviousActionsSectionAsync(int retroSessionId, int teamId, CancellationToken ct)
    {
        var previousSession = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(teamId, ct);
        if (previousSession is null) return null;

        var reviewedItems = await cardRepository.GetActionItemsReviewedInRetroSessionAsync(retroSessionId, ct);
        var unreviewedEligible = await cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousSession.Id, ct);

        // Only include the section when at least one action item from the previous retro was eligible
        if (reviewedItems.Count == 0 && unreviewedEligible.Count == 0) return null;

        var entries = new List<ReportPreviousActionEntry>();

        foreach (var card in reviewedItems)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, card.ReviewStatus.ToString()));

        foreach (var card in unreviewedEligible)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, "Not reviewed"));

        return new ReportPreviousActionsSection(previousSession.Name, entries);
    }
}
