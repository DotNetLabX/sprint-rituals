using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public record TicketCoverageInfo(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    string? ParentTicketKey,
    string IssueType,
    bool HasCoverage,
    int FailedRunCount,
    int TotalRunCount);

public class TestExecutionRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, TestExecution, string>(db)
{
    public override IQueryable<TestExecution> Query() =>
        Entity
            .Include(t => t.Links)
            .Include(t => t.TestRuns);

    public async Task<List<TestExecution>> GetByIssueIdsAsync(List<string> issueIds, CancellationToken ct = default) =>
        await Query()
            .Where(t => issueIds.Contains(t.Id))
            .ToListAsync(ct);

    public new async Task UpsertAsync(TestExecution testExecution, CancellationToken ct = default)
    {
        var existing = Entity.Local.SingleOrDefault(t => t.Id == testExecution.Id)
            ?? await Entity.SingleOrDefaultAsync(t => t.Id == testExecution.Id, ct);

        if (existing is null)
        {
            Entity.Add(testExecution);
        }
        else
        {
            existing.IssueKey = testExecution.IssueKey;
            existing.Summary = testExecution.Summary;
            existing.Status = testExecution.Status;
            existing.AssigneeId = testExecution.AssigneeId;
            existing.CreatedDate = testExecution.CreatedDate;
        }
    }

    public async Task ReplaceLinksAsync(string testExecutionIssueId, List<TestExecutionLink> links, CancellationToken ct = default)
    {
        var existing = await DbContext.TestExecutionLinks
            .Where(l => l.TestExecutionIssueId == testExecutionIssueId)
            .ToListAsync(ct);

        DbContext.TestExecutionLinks.RemoveRange(existing);
        DbContext.TestExecutionLinks.AddRange(links);
    }

    public async Task ReplaceTestRunsAsync(string testExecutionIssueId, List<TestRun> runs, CancellationToken ct = default)
    {
        var existing = await DbContext.TestRuns
            .Where(r => r.TestExecutionIssueId == testExecutionIssueId)
            .ToListAsync(ct);

        DbContext.TestRuns.RemoveRange(existing);
        DbContext.TestRuns.AddRange(runs);
    }

    /// <summary>
    /// Returns TestExecutions (with Links and TestRuns) attributed to the given sprint.
    /// Attribution: a TE belongs to the sprint whose date range contains its CreatedDate.
    /// If the TE was created in a gap between sprints, it is attributed to the most recently
    /// ended sprint before its creation date.
    /// The TE must also be linked (via TestExecutionLinks) to at least one ticket in the sprint.
    /// Cancelled TEs are excluded per BR7.
    /// </summary>
    public async Task<List<TestExecution>> GetTestExecutionsForSprintAsync(int sprintId, CancellationToken ct = default)
    {
        // Step 1: Find TEs linked to this sprint's tickets
        var teIdsLinkedToSprint = await DbContext.TestExecutionLinks
            .Where(l => DbContext.SprintMemberships.Any(sm => sm.SprintId == sprintId && sm.TicketId == l.TicketKey))
            .Select(l => l.TestExecutionIssueId)
            .Distinct()
            .ToListAsync(ct);

        if (teIdsLinkedToSprint.Count == 0)
            return [];

        // Step 2: Load sprint date ranges for creation-date attribution
        var sprintRanges = await LoadSprintRangesAsync(ct);

        // Step 3: Load TE creation dates
        var teCreatedDates = await DbContext.TestExecutions
            .Where(te => teIdsLinkedToSprint.Contains(te.Id))
            .Select(te => new { te.Id, te.CreatedDate })
            .ToListAsync(ct);

        // Step 4: Filter to TEs whose creation date falls in this sprint
        var targetTeIds = teCreatedDates
            .Where(te => AttributedSprintId(te.CreatedDate, sprintRanges) == sprintId)
            .Select(te => te.Id)
            .ToList();

        if (targetTeIds.Count == 0)
            return [];

        // "Cancelled" raw string required — EF Core cannot translate the computed IsCancelled property.
        return await Entity
            .Where(te => targetTeIds.Contains(te.Id) && te.Status != "Cancelled")
            .Include(te => te.Links)
                .ThenInclude(l => l.Ticket)
            .Include(te => te.TestRuns)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns feature tickets in the sprint's active scope along with coverage and failure data.
    /// Active scope: not removed, not bug, not excluded status, has effective SP, and IsStartedInSprint.
    /// Coverage and failure data is computed using non-cancelled TEs linked via Tests links.
    /// Sub-task coverage inheritance (BR9): sub-tasks without own TE links inherit from their parent ticket.
    /// </summary>
    public async Task<List<TicketCoverageInfo>> GetFeatureTicketsWithCoverageAsync(
        int sprintId,
        List<string> excludedStatuses,
        List<string> orderedStages,
        int startIndex,
        DateTime sprintStart,
        DateTime sprintEnd,
        int defaultSpPerBug,
        CancellationToken ct = default)
    {
        // Load sprint memberships with full ticket and assignee data
        var memberships = await DbContext.SprintMemberships
            .Where(sm => sm.SprintId == sprintId && sm.RemovedAt == null)
            .Include(sm => sm.Ticket)
                .ThenInclude(t => t!.Assignee)
            .ToListAsync(ct);

        // Filter to feature tickets with effective SP, not excluded status
        var featureMemberships = memberships
            .Where(sm =>
                sm.Ticket != null &&
                sm.Ticket.IssueType != "Bug" &&
                !excludedStatuses.Contains(sm.FinalStatus, StringComparer.OrdinalIgnoreCase) &&
                GetEffectiveSp(sm, defaultSpPerBug) != null)
            .ToList();

        if (featureMemberships.Count == 0)
            return [];

        var ticketIds = featureMemberships.Select(sm => sm.TicketId).ToList();

        // Load status transitions for these tickets to apply IsStartedInSprint filter
        var transitions = await DbContext.StatusTransitions
            .Where(st => ticketIds.Contains(st.TicketId))
            .ToListAsync(ct);

        // Apply IsStartedInSprint filter (replicate logic inline — TransitionAttributionChecker is in API layer)
        var startedMemberships = featureMemberships
            .Where(sm =>
            {
                if (startIndex < 0) return false;
                var ticketTransitions = transitions
                    .Where(t => t.TicketId == sm.TicketId)
                    .ToList();
                return ticketTransitions.Any(t =>
                    t.Timestamp >= sprintStart &&
                    t.Timestamp <= sprintEnd &&
                    GetStageIndex(t.ToStatus, orderedStages) >= startIndex);
            })
            .ToList();

        if (startedMemberships.Count == 0)
            return [];

        var startedTicketIds = startedMemberships.Select(sm => sm.TicketId).ToList();

        // Load all non-cancelled TE links for these tickets (Tests links for coverage)
        var teLinks = await DbContext.TestExecutionLinks
            .Where(l => startedTicketIds.Contains(l.TicketKey))
            .Join(DbContext.TestExecutions.Where(te => te.Status != "Cancelled"),
                l => l.TestExecutionIssueId,
                te => te.Id,
                (l, te) => new { l.TicketKey, l.TestExecutionIssueId, l.LinkType })
            .ToListAsync(ct);

        // Load test runs for these TEs
        var teIdsWithLinks = teLinks.Select(l => l.TestExecutionIssueId).Distinct().ToList();
        var testRuns = teIdsWithLinks.Count > 0
            ? await DbContext.TestRuns
                .Where(r => teIdsWithLinks.Contains(r.TestExecutionIssueId))
                .ToListAsync(ct)
            : [];

        // Build coverage and run count per ticket
        var testsLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Tests)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var runsByTe = testRuns
            .GroupBy(r => r.TestExecutionIssueId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return startedMemberships.Select(sm =>
        {
            var ticketKey = sm.TicketId;
            var ticket = sm.Ticket!;

            // Get TE IDs for this ticket (own links)
            var ownTeIds = testsLinksByTicket.GetValueOrDefault(ticketKey, []);

            // BR9: if no own links, check parent ticket's links
            var effectiveTeIds = ownTeIds.Count > 0
                ? ownTeIds
                : (!string.IsNullOrEmpty(ticket.ParentTicketKey)
                    ? testsLinksByTicket.GetValueOrDefault(ticket.ParentTicketKey, [])
                    : []);

            var hasCoverage = effectiveTeIds.Count > 0;

            // Compute run counts across all effective TEs
            var failedRunCount = 0;
            var totalRunCount = 0;
            foreach (var teId in effectiveTeIds)
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                failedRunCount += runs.Count(r => r.Status == TestRunStatus.Fail);
                totalRunCount += runs.Count(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            }

            return new TicketCoverageInfo(
                TicketKey: ticketKey,
                Summary: ticket.Summary,
                AssigneeName: ticket.Assignee?.DisplayName,
                StoryPoints: sm.StoryPoints,
                ParentTicketKey: ticket.ParentTicketKey,
                IssueType: ticket.IssueType,
                HasCoverage: hasCoverage,
                FailedRunCount: failedRunCount,
                TotalRunCount: totalRunCount);
        }).ToList();
    }

    private async Task<List<(int Id, DateTime StartDate, DateTime EndDate)>> LoadSprintRangesAsync(CancellationToken ct)
    {
        var raw = await DbContext.Sprints
            .OrderBy(s => s.StartDate)
            .Select(s => new { s.Id, s.StartDate, s.EndDate })
            .ToListAsync(ct);
        return raw.Select(s => (s.Id, s.StartDate, s.EndDate)).ToList();
    }

    /// <summary>
    /// Attributes a TE to a sprint based on its creation date.
    /// Finds the sprint whose date range contains the date; if the date falls in a gap between
    /// sprints, uses the most recently ended sprint before that date.
    /// </summary>
    private static int AttributedSprintId(DateTime createdDate, List<(int Id, DateTime StartDate, DateTime EndDate)> sprints)
    {
        if (sprints.Count == 0) return 0;

        // Find sprint containing the creation date
        foreach (var s in sprints)
        {
            if (createdDate >= s.StartDate && createdDate <= s.EndDate)
                return s.Id;
        }

        // Gap: attribute to the most recently started sprint before creation date
        var preceding = sprints.LastOrDefault(s => s.StartDate <= createdDate);
        if (preceding.Id != 0) return preceding.Id;

        // TE predates all sprints: attribute to first sprint
        return sprints[0].Id;
    }

    private static decimal? GetEffectiveSp(SprintMembership sm, int defaultSpPerBug)
    {
        if (sm.StoryPoints.HasValue && sm.StoryPoints.Value > 0)
            return sm.StoryPoints;
        if (sm.Ticket?.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    private static int GetStageIndex(string status, List<string> orderedStages) =>
        orderedStages.FindIndex(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the set of sprint IDs (from the provided list) that have at least one non-cancelled
    /// TestExecution attributed to that sprint by creation-date attribution.
    /// Mirrors the semantics of GetTestExecutionsForSprintAsync.
    /// </summary>
    public async Task<HashSet<int>> GetSprintIdsWithQaDataAsync(List<int> sprintIds, CancellationToken ct = default)
    {
        if (sprintIds.Count == 0)
            return [];

        // Step 1: Find TEs linked to any ticket in the candidate sprints
        var teIdsInCandidates = await DbContext.TestExecutionLinks
            .Where(l => DbContext.SprintMemberships.Any(sm => sprintIds.Contains(sm.SprintId) && sm.TicketId == l.TicketKey))
            .Select(l => l.TestExecutionIssueId)
            .Distinct()
            .ToListAsync(ct);

        if (teIdsInCandidates.Count == 0)
            return [];

        // Step 2: Load sprint date ranges for attribution
        var sprintRanges = await LoadSprintRangesAsync(ct);

        // Step 3: Load creation dates and filter non-cancelled
        var nonCancelledTes = await DbContext.TestExecutions
            .Where(te => teIdsInCandidates.Contains(te.Id) && te.Status != "Cancelled")
            .Select(te => new { te.Id, te.CreatedDate })
            .ToListAsync(ct);

        // Step 4: Attribute each TE to a sprint; collect those in the candidate set
        var candidateSet = sprintIds.ToHashSet();
        var result = new HashSet<int>();
        foreach (var te in nonCancelledTes)
        {
            var attributed = AttributedSprintId(te.CreatedDate, sprintRanges);
            if (candidateSet.Contains(attributed))
                result.Add(attributed);
        }

        return result;
    }

    /// <summary>
    /// Returns TE links and test runs for the given ticket keys, used for epic-level QA metric computation.
    /// Only non-cancelled TEs are included (Status != "Cancelled").
    /// Returns:
    ///   testsLinksByTicket  — Tests links grouped by ticket key (coverage)
    ///   blocksLinksByTicket — Blocks links grouped by ticket key (bugs found)
    ///   runsByTeId          — TestRuns grouped by TE issue ID
    /// </summary>
    public async Task<(
        Dictionary<string, List<string>> TestsLinksByTicket,
        Dictionary<string, List<string>> BlocksLinksByTicket,
        Dictionary<string, List<TestRun>> RunsByTeId)>
        GetTestExecutionDataForTicketsAsync(List<string> ticketKeys, CancellationToken ct = default)
    {
        if (ticketKeys.Count == 0)
            return ([], [], []);

        // Load all non-cancelled TE links for these tickets (both Tests and Blocks link types)
        var teLinks = await DbContext.TestExecutionLinks
            .Where(l => ticketKeys.Contains(l.TicketKey))
            .Join(DbContext.TestExecutions.Where(te => te.Status != "Cancelled"),
                l => l.TestExecutionIssueId,
                te => te.Id,
                (l, te) => new { l.TicketKey, l.TestExecutionIssueId, l.LinkType })
            .ToListAsync(ct);

        // Load test runs for all involved TEs in one query
        var teIds = teLinks.Select(l => l.TestExecutionIssueId).Distinct().ToList();
        var testRuns = teIds.Count > 0
            ? await DbContext.TestRuns
                .Where(r => teIds.Contains(r.TestExecutionIssueId))
                .ToListAsync(ct)
            : new List<TestRun>();

        var testsLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Tests)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var blocksLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Blocks)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var runsByTeId = testRuns
            .GroupBy(r => r.TestExecutionIssueId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return (testsLinksByTicket, blocksLinksByTicket, runsByTeId);
    }

    public async Task UpsertTestSetAsync(TestSet testSet, CancellationToken ct = default)
    {
        var existing = DbContext.TestSets.Local.SingleOrDefault(t => t.Id == testSet.Id)
            ?? await DbContext.TestSets.SingleOrDefaultAsync(t => t.Id == testSet.Id, ct);

        if (existing is null)
        {
            DbContext.TestSets.Add(testSet);
        }
        else
        {
            existing.IssueKey = testSet.IssueKey;
            existing.Summary = testSet.Summary;
            existing.AssigneeId = testSet.AssigneeId;
            existing.Status = testSet.Status;
        }
    }
}
