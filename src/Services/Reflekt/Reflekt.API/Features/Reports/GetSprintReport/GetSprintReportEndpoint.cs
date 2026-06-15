using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Reports.GetSprintReport;

public sealed record GetSprintReportRequest(int TeamId, int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/teams/{teamId}/retro-sessions/{retroSessionId}/report")]
[Tags("Reports")]
public sealed class GetSprintReportEndpoint(
    TeamRepository teamRepository,
    RetroSessionRepository retroSessionRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetSprintReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetSprintReportRequest req, CancellationToken ct)
    {
        var sprint = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (sprint is null || sprint.TeamId != req.TeamId) { await Send.NotFoundAsync(ct); return; }

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            // "sprintId" claim is a SignalR/guest-token wire value — stays sprintId-named (Q2 DO-NOT-TOUCH)
            if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != req.RetroSessionId)
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

        var cards = await cardRepository.GetBySprintIdAsync(req.RetroSessionId, ct);
        var groups = await cardGroupRepository.GetBySprintIdAsync(req.RetroSessionId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetBySprintIdAsync(req.RetroSessionId, ct);
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
            BuildColumnSection(sprint.Column1Name, sprint.Column1Color.Value, ColumnType.Column1, groupEntries),
            BuildColumnSection(sprint.Column2Name, sprint.Column2Color.Value, ColumnType.Column2, groupEntries),
            BuildColumnSection(sprint.Column3Name, sprint.Column3Color.Value, ColumnType.Column3, groupEntries),
        };

        var actionItems = cards
            .Where(c => c.CardType == CardType.ActionItem)
            .Select(c => c.Adapt<ReportActionItemEntry>())
            .ToList();

        var previousActionsSection = await BuildPreviousActionsSectionAsync(req.RetroSessionId, sprint.TeamId, ct);

        var response = new ReportResponse(
            new ReportSprintInfo(sprint.Id, sprint.Name, sprint.CreatedAt, sprint.ClosedAt),
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
        var previousSprint = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(teamId, ct);
        if (previousSprint is null) return null;

        var reviewedItems = await cardRepository.GetActionItemsReviewedInSprintAsync(retroSessionId, ct);
        var unreviewedEligible = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);

        // Only include the section when at least one action item from the previous sprint was eligible
        if (reviewedItems.Count == 0 && unreviewedEligible.Count == 0) return null;

        var entries = new List<ReportPreviousActionEntry>();

        foreach (var card in reviewedItems)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, card.ReviewStatus.ToString()));

        foreach (var card in unreviewedEligible)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, "Not reviewed"));

        return new ReportPreviousActionsSection(previousSprint.Name, entries);
    }
}
