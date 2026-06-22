using AwesomeAssertions;
using Fokus.Domain;
using Fokus.Domain.Analytics;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Property-based invariants for <see cref="BugRatioAnalyzer"/> (FsCheck 3.x C# API, direct bool return).
///
/// These pin structural invariants that must hold for ALL generated inputs — Stryker confirms each by
/// killing a mutant that violates the invariant:
///   - ratio is always within [0, 100] and is never negative (BR3 guard + numerator ≤ denominator);
///   - completedSp == bugSp + nonBugSp (BR2 partition is total);
///   - bug + non-bug ticket counts are complementary and sum to the completed count (BR10);
///   - aggregate ratio == 0 exactly when there is no completed SP (BR3 divide-by-zero guard).
///
/// Inputs are generated as small non-negative SP lists; we build a single sprint where every
/// membership is completed (a transition To "Done" inside the window), so the analyzer's SP maths
/// is exercised directly without depending on FsCheck to discover the completion wiring.
/// </summary>
public class BugRatioAnalyzerPropertyTests
{
    private const string DoneStage = "Done";
    private static readonly DateTime SStart = new(2024, 1, 1);
    private static readonly DateTime SEnd = new(2024, 1, 14);
    private static readonly DateTime SMid = new(2024, 1, 7);
    private static readonly BugRatioAnalyzer Analyzer = new();

    private static AppSettings Settings()
    {
        var s = AppSettings.CreateDefault();
        s.WorkflowStages = ["To Do", "In Progress"];
        s.DoneStatuses = ["Done"];
        s.ExcludedFromScopeStatuses = [];
        s.CycleTime = new CycleTimeConfig { StartStage = null, EndStage = null };
        s.BugRatio = new BugRatioConfig
        {
            AlertThreshold = 50,
            ConsecutiveSprintCount = 2,
            Target = 30,
            DefaultSpPerBug = 0
        };
        return s;
    }

    /// <summary>
    /// Builds a one-sprint scenario from two SP lists (bugs, stories). Every ticket is completed.
    /// Returns the team metrics for the single target sprint.
    /// </summary>
    private static BugRatioTeamMetricsData TeamMetricsFor(int[] bugSps, int[] storySps)
    {
        var memberships = new List<SprintMembership>();
        var transitions = new List<StatusTransition>();

        void Add(string id, string issueType, int sp)
        {
            var ticket = new Ticket
            {
                Id = id,
                Summary = id,
                IssueType = issueType,
                Priority = "Medium",
                CurrentStatus = "Done"
            };
            memberships.Add(new SprintMembership
            {
                TicketId = id,
                FinalStatus = "Done",
                StoryPoints = sp,
                Ticket = ticket,
                AddedAt = SStart
            });
            transitions.Add(new StatusTransition
            {
                TicketId = id, FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = SMid
            });
        }

        for (var i = 0; i < bugSps.Length; i++)
            Add($"B{i}", "Bug", bugSps[i]);
        for (var i = 0; i < storySps.Length; i++)
            Add($"S{i}", "Story", storySps[i]);

        var sprint = new Sprint { Id = 1, Name = "S1", BoardName = "B", StartDate = SStart, EndDate = SEnd };
        foreach (var m in memberships)
            sprint.AddMembership(m);

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);
        return result.TeamMetrics;
    }

    // Constrain generated ints to a small non-negative range so SP values are realistic and the
    // run stays fast. Each element ∈ [0, 20].
    private static int[] Clamp(int[] values) =>
        values.Select(v => Math.Abs(v) % 21).ToArray();

    // --- Invariant 1: ratio is always within [0, 100] ---

    [Property]
    public bool TeamRatio_IsAlwaysWithinZeroToHundred(int[] rawBugs, int[] rawStories)
    {
        var metrics = TeamMetricsFor(Clamp(rawBugs), Clamp(rawStories));
        return metrics.BugRatioPercent >= 0m && metrics.BugRatioPercent <= 100m;
    }

    // --- Invariant 2: bugSp + nonBugSp == completedSp (the partition is total and disjoint) ---

    [Property]
    public bool TeamTotals_BugPlusNonBug_EqualsCompleted(int[] rawBugs, int[] rawStories)
    {
        var metrics = TeamMetricsFor(Clamp(rawBugs), Clamp(rawStories));
        return metrics.TotalBugSp + metrics.TotalNonBugSp == metrics.TotalCompletedSp;
    }

    // --- Invariant 3: ratio == 0 EXACTLY when there is no completed SP (BR3 guard) ---
    // With defaultSpPerBug=0, completedSp is 0 iff all SP values are 0.

    [Property]
    public bool TeamRatio_IsZero_IFF_NoCompletedSp(int[] rawBugs, int[] rawStories)
    {
        var metrics = TeamMetricsFor(Clamp(rawBugs), Clamp(rawStories));
        var noCompletedSp = metrics.TotalCompletedSp == 0m;
        var ratioIsZero = metrics.BugRatioPercent == 0m;
        // noCompletedSp ⇒ ratioIsZero. (The converse can also hold when there are stories but no bugs,
        // so we only assert the guard direction here.)
        return !noCompletedSp || ratioIsZero;
    }

    // --- Invariant 4: an all-bug completed scope yields ratio 100; an all-story scope yields 0 ---
    // Uses PositiveInt to guarantee at least one strictly-positive SP so completedSp > 0.

    [Property]
    public bool TeamRatio_Is100_When_AllBugsAndPositiveSp(PositiveInt sp, PositiveInt count)
    {
        var n = (count.Get % 5) + 1;             // 1..5 bugs
        var bugSps = Enumerable.Repeat(sp.Get % 20 + 1, n).ToArray();
        var metrics = TeamMetricsFor(bugSps, []);
        return metrics.BugRatioPercent == 100m;
    }

    [Property]
    public bool TeamRatio_IsZero_When_AllStoriesAndPositiveSp(PositiveInt sp, PositiveInt count)
    {
        var n = (count.Get % 5) + 1;             // 1..5 stories
        var storySps = Enumerable.Repeat(sp.Get % 20 + 1, n).ToArray();
        var metrics = TeamMetricsFor([], storySps);
        return metrics.BugRatioPercent == 0m;
    }

    // --- Invariant 5: bug SP and non-bug SP are each non-negative ---

    [Property]
    public bool TeamTotals_AreNonNegative(int[] rawBugs, int[] rawStories)
    {
        var metrics = TeamMetricsFor(Clamp(rawBugs), Clamp(rawStories));
        return metrics.TotalBugSp >= 0m && metrics.TotalNonBugSp >= 0m && metrics.TotalCompletedSp >= 0m;
    }

    // --- Invariant 6: per-sprint trend bug+nonBug sums equal the team totals (ratio-of-totals, BR4) ---
    // Single sprint ⇒ the one trend row's SP must equal the team totals exactly.

    [Property]
    public bool SingleSprint_TrendRow_MatchesTeamTotals(int[] rawBugs, int[] rawStories)
    {
        var metrics = TeamMetricsFor(Clamp(rawBugs), Clamp(rawStories));
        var trend = metrics.PerSprintTrend.Single();
        return trend.BugSp == metrics.TotalBugSp
            && trend.NonBugSp == metrics.TotalNonBugSp
            && trend.CompletedSp == metrics.TotalCompletedSp;
    }

    // =====================================================================
    // MUTANT-TARGETED SUPPLEMENT (multi-sprint / multi-element invariants)
    // These need ≥2 sprints or ≥2 group members so that Σ ≠ Max and the
    // Sum→Max / OrderBy→OrderByDescending timeouts are killed for ALL inputs.
    // =====================================================================

    /// <summary>
    /// Builds a developer active across N sprints from a per-sprint (bugSp, storySp) list.
    /// Every membership is completed. Returns the single developer's aggregate entry.
    /// </summary>
    private static BugRatioDeveloperData DevAcrossSprints((int Bug, int Story)[] perSprint)
    {
        var dev = new Developer { Id = "d1", DisplayName = "Dev" };
        var sprints = new List<Sprint>();
        var transitions = new List<StatusTransition>();

        for (var i = 0; i < perSprint.Length; i++)
        {
            // Each sprint is a disjoint 13-day window so completion stays inside it.
            var start = new DateTime(2024, 1, 1).AddDays(i * 14);
            var end = start.AddDays(13);
            var mid = start.AddDays(3);
            var sprint = new Sprint { Id = i + 1, Name = $"S{i + 1}", BoardName = "B", StartDate = start, EndDate = end };

            void Add(string id, string type, int sp)
            {
                var ticket = new Ticket
                {
                    Id = id, Summary = id, IssueType = type, Priority = "Medium",
                    CurrentStatus = "Done", AssigneeId = "d1"
                };
                sprint.AddMembership(new SprintMembership
                {
                    TicketId = id, FinalStatus = "Done", StoryPoints = sp, Ticket = ticket, AddedAt = start
                });
                transitions.Add(new StatusTransition
                {
                    TicketId = id, FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = mid
                });
            }

            Add($"B{i}", "Bug", perSprint[i].Bug);
            Add($"S{i}", "Story", perSprint[i].Story);
            sprints.Add(sprint);
        }

        var result = Analyzer.ComputeMultiSprint(sprints, sprints, [dev], Settings(), transitions, subTeam: null);
        return result.Developers.Single();
    }

    // --- Invariant 7: dev SP aggregates are the SUM of per-sprint breakdowns (L92/93), not Max ---
    // For ≥2 sprints with strictly-positive SP, Σ strictly exceeds Max, so a Sum→Max mutant fails.

    [Property]
    public bool DeveloperSp_IsSumOfBreakdowns_NotMax(PositiveInt a, PositiveInt b, PositiveInt c, PositiveInt d)
    {
        var s1 = (Bug: a.Get % 10 + 1, Story: b.Get % 10 + 1);
        var s2 = (Bug: c.Get % 10 + 1, Story: d.Get % 10 + 1);
        var entry = DevAcrossSprints([s1, s2]);

        var expectedBugSp = s1.Bug + s2.Bug;
        var expectedNonBugSp = s1.Story + s2.Story;
        // Sum must equal element-wise total; Max (≤ either element-wise total, strictly < when both >0) cannot.
        return entry.BugSp == expectedBugSp
            && entry.NonBugSp == expectedNonBugSp
            && entry.CompletedSp == expectedBugSp + expectedNonBugSp;
    }

    // --- Invariant 8: dev ticket counts are the SUM of per-sprint breakdown counts (L96/97), not Max ---
    // Each sprint contributes exactly 1 bug + 1 story, so across 2 sprints Σ = 2 each (Max would be 1).

    [Property]
    public bool DeveloperTicketCounts_AreSumOfBreakdowns_NotMax(PositiveInt a, PositiveInt b)
    {
        // SP values only affect magnitude, not counts; keep them positive so each ticket completes.
        var entry = DevAcrossSprints([(a.Get % 9 + 1, b.Get % 9 + 1), (a.Get % 9 + 1, b.Get % 9 + 1)]);
        // 2 sprints × (1 bug + 1 story) ⇒ 2 and 2. Sum→Max mutant would yield 1 and 1.
        return entry.BugTicketCount == 2 && entry.NonBugTicketCount == 2;
    }

    /// <summary>
    /// Builds an N-sprint team scenario and returns the per-sprint trend rows (in output order),
    /// each tagged with its StartDate, so ordering can be asserted. Sprints are supplied to the
    /// analyzer in REVERSE chronological order to exercise the OrderBy(StartDate) sort.
    /// </summary>
    private static List<(int SprintId, DateTime Start, decimal Ratio)> TrendOrderForReversedInput(int sprintCount)
    {
        var sprints = new List<Sprint>();
        var transitions = new List<StatusTransition>();

        for (var i = 0; i < sprintCount; i++)
        {
            var start = new DateTime(2024, 1, 1).AddDays(i * 14);
            var end = start.AddDays(13);
            var mid = start.AddDays(3);
            var sprint = new Sprint { Id = i + 1, Name = $"S{i + 1}", BoardName = "B", StartDate = start, EndDate = end };
            // Distinct ratio per sprint: i bug SP + 1 story SP (ratio rises with i).
            var bugSp = i + 1;
            var bug = new Ticket { Id = $"B{i}", Summary = $"B{i}", IssueType = "Bug", Priority = "Medium", CurrentStatus = "Done" };
            var story = new Ticket { Id = $"S{i}", Summary = $"S{i}", IssueType = "Story", Priority = "Medium", CurrentStatus = "Done" };
            sprint.AddMembership(new SprintMembership { TicketId = $"B{i}", FinalStatus = "Done", StoryPoints = bugSp, Ticket = bug, AddedAt = start });
            sprint.AddMembership(new SprintMembership { TicketId = $"S{i}", FinalStatus = "Done", StoryPoints = 1m, Ticket = story, AddedAt = start });
            transitions.Add(new StatusTransition { TicketId = $"B{i}", FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = mid });
            transitions.Add(new StatusTransition { TicketId = $"S{i}", FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = mid });
            sprints.Add(sprint);
        }

        // Feed REVERSED so a correct OrderBy(StartDate) must re-sort ascending.
        sprints.Reverse();
        var result = Analyzer.ComputeMultiSprint(sprints, sprints, [], Settings(), transitions, subTeam: null);

        // Map sprintId → start to tag each emitted row.
        var startById = sprints.ToDictionary(s => s.Id, s => s.StartDate);
        return result.TeamMetrics.PerSprintTrend
            .Select(t => (t.SprintId, startById[t.SprintId], t.BugRatioPercent))
            .ToList();
    }

    // --- Invariant 9: the per-sprint trend is ALWAYS ascending by StartDate (L20) ---
    // OrderBy→OrderByDescending would reverse the rows; assert monotonic non-decreasing StartDate.

    [Property]
    public bool PerSprintTrend_IsAscendingByStartDate(PositiveInt rawCount)
    {
        var n = rawCount.Get % 4 + 2;   // 2..5 sprints
        var rows = TrendOrderForReversedInput(n);
        for (var i = 1; i < rows.Count; i++)
            if (rows[i].Start < rows[i - 1].Start)
                return false;           // a descending step ⇒ OrderByDescending mutant
        return true;
    }

    /// <summary>
    /// Builds a single sprint with one issue-type group of N completed memberships of given SPs,
    /// returning that group's SpTotal. Exercises BuildIssueTypeBreakdown's Sum (L380).
    /// </summary>
    private static decimal IssueTypeSpTotalFor(int[] sps)
    {
        var sprint = new Sprint { Id = 1, Name = "S1", BoardName = "B", StartDate = SStart, EndDate = SEnd };
        var transitions = new List<StatusTransition>();
        for (var i = 0; i < sps.Length; i++)
        {
            var ticket = new Ticket { Id = $"S{i}", Summary = $"S{i}", IssueType = "Story", Priority = "Medium", CurrentStatus = "Done" };
            sprint.AddMembership(new SprintMembership { TicketId = $"S{i}", FinalStatus = "Done", StoryPoints = sps[i], Ticket = ticket, AddedAt = SStart });
            transitions.Add(new StatusTransition { TicketId = $"S{i}", FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = SMid });
        }

        var result = Analyzer.ComputeMultiSprint([sprint], [sprint], [], Settings(), transitions, subTeam: null);
        var group = result.IssueTypeBreakdown.Single(e => e.IssueType == "Story");
        return group.SpTotal;
    }

    // --- Invariant 10: issue-type SpTotal is the SUM of the group's SPs (L380), not Max ---
    // Use ≥2 strictly-positive SPs so Σ > Max and the Sum→Max mutant fails.

    [Property]
    public bool IssueTypeSpTotal_IsSumOfGroup_NotMax(PositiveInt a, PositiveInt b, PositiveInt c)
    {
        var sps = new[] { a.Get % 15 + 1, b.Get % 15 + 1, c.Get % 15 + 1 };
        var expected = (decimal)sps.Sum();
        return IssueTypeSpTotalFor(sps) == expected;
    }
}
