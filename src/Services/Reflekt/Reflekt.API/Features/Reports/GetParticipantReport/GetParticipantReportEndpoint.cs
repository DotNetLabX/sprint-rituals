using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Reports.GetParticipantReport;

public sealed record GetParticipantReportRequest(int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/sprints/{sprintId}/report")]
[Tags("Reports")]
public sealed class GetParticipantReportEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetParticipantReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetParticipantReportRequest req, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
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
        var groups = await cardGroupRepository.GetBySprintIdAsync(req.SprintId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetBySprintIdAsync(req.SprintId, ct);
        var teamInfo = await teamRepository.GetByIdAsync(sprint.TeamId, ct);

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

        var previousActionsSection = await BuildPreviousActionsSectionAsync(req.SprintId, sprint.TeamId, ct);

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

    private async Task<ReportPreviousActionsSection?> BuildPreviousActionsSectionAsync(int sprintId, int teamId, CancellationToken ct)
    {
        var previousSprint = await sprintRepository.GetMostRecentClosedByTeamIdAsync(teamId, ct);
        if (previousSprint is null) return null;

        var reviewedItems = await cardRepository.GetActionItemsReviewedInSprintAsync(sprintId, ct);
        var unreviewedEligible = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);

        if (reviewedItems.Count == 0 && unreviewedEligible.Count == 0) return null;

        var entries = new List<ReportPreviousActionEntry>();

        foreach (var card in reviewedItems)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, card.ReviewStatus.ToString()));

        foreach (var card in unreviewedEligible)
            entries.Add(new ReportPreviousActionEntry(card.Text, card.AssigneeName, "Not reviewed"));

        return new ReportPreviousActionsSection(previousSprint.Name, entries);
    }
}
