using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;

namespace Reflekt.Domain.Reports;

/// <summary>
/// Pure domain service that assembles a retro report from pre-loaded aggregates (ADR-005).
/// The handler loads all aggregates and passes them in; this class contains no DB/repo/HTTP dependency.
/// </summary>
public class RetroReportBuilder
{
    /// <summary>
    /// Builds a RetroReportData from pre-loaded aggregates.
    /// </summary>
    /// <param name="session">The retro session being reported on.</param>
    /// <param name="cards">All cards for the session.</param>
    /// <param name="groups">All card groups for the session.</param>
    /// <param name="voteCountsByGroupId">Vote counts keyed by group id.</param>
    /// <param name="participants">All participants for the session.</param>
    /// <param name="team">The team that owns the session.</param>
    /// <param name="reviewedPreviousActions">Action items reviewed in this session (from GetActionItemsReviewedInRetroSessionAsync).</param>
    /// <param name="unreviewedEligiblePreviousActions">Unreviewed action items from the previous session (from GetUnreviewedActionItemsByRetroSessionIdAsync).</param>
    /// <param name="previousSession">The most-recent closed session for this team; null means no previous-actions section.</param>
    public RetroReportData Build(
        RetroSession session,
        IReadOnlyList<Card> cards,
        IReadOnlyList<CardGroup> groups,
        IReadOnlyDictionary<int, int> voteCountsByGroupId,
        IReadOnlyList<Participant> participants,
        Team team,
        IReadOnlyList<Card> reviewedPreviousActions,
        IReadOnlyList<Card> unreviewedEligiblePreviousActions,
        RetroSession? previousSession)
    {
        // Group Note cards by their GroupId, ordered by CreatedAt (mirrors endpoint lines 55–58)
        var cardsByGroup = cards
            .Where(c => c.CardType == CardType.Note && c.GroupId.HasValue)
            .GroupBy(c => c.GroupId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Card>)g.OrderBy(c => c.CreatedAt).ToList());

        // Build filtered, ordered group entries (mirrors endpoint lines 60–71)
        var groupEntries = groups
            .Select(g =>
            {
                var votes = voteCountsByGroupId.GetValueOrDefault(g.Id, 0);
                var groupCards = cardsByGroup.GetValueOrDefault(g.Id, []);
                var title = g.Title ?? groupCards.FirstOrDefault()?.Text ?? "";
                var isDiscussed = g.DiscussedAt.HasValue;
                return (Group: g, Title: title, Votes: votes, Cards: groupCards, IsDiscussed: isDiscussed);
            })
            .Where(x => x.IsDiscussed || x.Votes > 1)
            .OrderByDescending(x => x.Votes)
            .ToList();

        // Build one column section per retro column (uses RR-B VOs — mirrors endpoint lines 73–78)
        var columns = new List<ReportColumnSectionData>
        {
            BuildColumnSection(session.Column1.Name, session.Column1.Color.Value, ColumnType.Column1, groupEntries),
            BuildColumnSection(session.Column2.Name, session.Column2.Color.Value, ColumnType.Column2, groupEntries),
            BuildColumnSection(session.Column3.Name, session.Column3.Color.Value, ColumnType.Column3, groupEntries),
        };

        // Collect action items (mirrors endpoint lines 80–83)
        var actionItems = cards
            .Where(c => c.CardType == CardType.ActionItem)
            .Select(BuildActionItemData)
            .ToList();

        var previousActionsSection = BuildPreviousActionsSection(
            previousSession, reviewedPreviousActions, unreviewedEligiblePreviousActions);

        return new RetroReportData(
            new ReportRetroSessionInfoData(session.Id, session.Name, session.CreatedAt, session.ClosedAt),
            new ReportTeamInfoData(team.Id, team.Name),
            participants.Select(p => new ReportParticipantInfoData(p.DisplayName)).ToList(),
            columns,
            actionItems,
            previousActionsSection);
    }

    // --- Private helpers ---

    private static ReportColumnSectionData BuildColumnSection(
        string columnName,
        string columnColor,
        ColumnType columnType,
        List<(CardGroup Group, string Title, int Votes, IReadOnlyList<Card> Cards, bool IsDiscussed)> groupEntries)
    {
        var groups = groupEntries
            .Where(x => x.Group.ColumnType == columnType)
            .Select(x => new ReportGroupEntryData(
                x.Title,
                x.Votes,
                x.IsDiscussed,
                x.Cards.Select(BuildCardData).ToList()))
            .ToList();
        return new ReportColumnSectionData(columnName, columnColor, groups);
    }

    private static ReportCardEntryData BuildCardData(Card card) =>
        new(card.Text, BuildEvidenceLinks(card.EvidenceLinks));

    private static ReportActionItemData BuildActionItemData(Card card) =>
        new(card.Text, card.AssigneeName, BuildEvidenceLinks(card.EvidenceLinks));

    // Projects evidence links in list order, carrying only the four fields that the wire record exposes.
    // Description, IconUrl, ImageUrl, FetchStatus, FetchedAt are intentionally dropped — they have no
    // matching member on the wire record and must not be surfaced to avoid silent JSON drift.
    private static IReadOnlyList<ReportEvidenceLinkData> BuildEvidenceLinks(IReadOnlyList<EvidenceLink> links) =>
        links.Select(l => new ReportEvidenceLinkData(l.Url, l.Source, l.Title, l.ExternalKey)).ToList();

    // Assembles the previous-actions section from pre-loaded reviewed and unreviewed-eligible items.
    // Returns null when no previous session exists or both lists are empty (same rule as the original
    // BuildPreviousActionsSectionAsync — now synchronous because data is pre-loaded by the handler).
    private static ReportPreviousActionsData? BuildPreviousActionsSection(
        RetroSession? previousSession,
        IReadOnlyList<Card> reviewedItems,
        IReadOnlyList<Card> unreviewedEligible)
    {
        if (previousSession is null) return null;
        if (reviewedItems.Count == 0 && unreviewedEligible.Count == 0) return null;

        var entries = new List<ReportPreviousActionData>();

        foreach (var card in reviewedItems)
            entries.Add(new ReportPreviousActionData(card.Text, card.AssigneeName, card.ReviewStatus.ToString()));

        foreach (var card in unreviewedEligible)
            entries.Add(new ReportPreviousActionData(card.Text, card.AssigneeName, "Not reviewed"));

        return new ReportPreviousActionsData(previousSession.Name, entries);
    }
}
