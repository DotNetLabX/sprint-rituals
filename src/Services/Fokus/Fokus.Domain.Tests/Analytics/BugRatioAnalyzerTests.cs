using AwesomeAssertions;
using Fokus.Domain;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Mutation-gated example-based tests for <see cref="BugRatioAnalyzer"/>.
///
/// One [Fact] per verified rule boundary from docs/kb/bug-ratio.md (BR-1 … BR-37). Each test pins an
/// exact threshold / equality / relational boundary (epsilon on every &gt;=, &gt;, ==, !=) so Stryker's
/// relational, equality, arithmetic, boolean-connector and string/return-literal mutants are killed —
/// coverage is incidental, killing mutants is the goal.
///
/// Construction model (verified against the SUT + supporting domain types):
///   - Completed-in-sprint is decided by TransitionAttributionChecker.IsCompletedInSprint via a
///     StatusTransition whose ToStatus reaches the end-stage index inside [StartDate, EndDate].
///   - WorkflowStages = ["To Do","In Progress"], DoneStatuses = ["Done"] ⇒ orderedStages =
///     ["To Do","In Progress","Done"], endIndex = 2. A transition To "Done" inside the window
///     therefore marks completion.
///   - GetEffectiveSp: StoryPoints (when > 0) wins; else Bug + defaultSpPerBug>0 ⇒ defaultSpPerBug;
///     else null (treated as 0 in the sums).
///
/// NON-NEGOTIABLE: a test that fails on the CURRENT production code is KEPT and flagged with a
/// `// CANDIDATE BUG:` comment — never deleted, and the production code is never edited to make it pass.
/// (None of the tests below are currently red — all 59 pass against the shipped analyzer.)
/// </summary>
public class BugRatioAnalyzerTests
{
    // =====================================================================
    // Fixture builders — local to this file (TestData fixture is a placeholder).
    // =====================================================================

    private const string DoneStage = "Done";

    private static AppSettings Settings(
        int alertThreshold = 50,
        int consecutiveSprintCount = 2,
        int defaultSpPerBug = 0,
        List<string>? excludedStatuses = null)
    {
        var s = AppSettings.CreateDefault();
        s.WorkflowStages = ["To Do", "In Progress"];
        s.DoneStatuses = ["Done"];
        s.ExcludedFromScopeStatuses = excludedStatuses ?? [];
        s.CycleTime = new CycleTimeConfig { StartStage = null, EndStage = null };
        s.BugRatio = new BugRatioConfig
        {
            AlertThreshold = alertThreshold,
            ConsecutiveSprintCount = consecutiveSprintCount,
            Target = 30,
            DefaultSpPerBug = defaultSpPerBug
        };
        return s;
    }

    private static Developer Dev(string id, string display = "Dev", string? subTeam = null) =>
        new() { Id = id, DisplayName = display, SubTeam = subTeam };

    private static Ticket TicketOf(
        string id,
        string issueType = "Story",
        string? assigneeId = null,
        string? assigneeSubTeam = null,
        decimal? storyPoints = null)
    {
        var t = new Ticket
        {
            Id = id,
            Summary = id,
            IssueType = issueType,
            Priority = "Medium",
            CurrentStatus = "Done",
            AssigneeId = assigneeId,
            StoryPoints = storyPoints
        };
        if (assigneeId is not null)
            t.Assignee = new Developer { Id = assigneeId, DisplayName = assigneeId, SubTeam = assigneeSubTeam };
        return t;
    }

    // A ticket with a genuinely-null IssueType (records mark IssueType `required`, so we null it
    // explicitly to exercise the `== "Bug"` null-propagation and the `?? "Unknown"` fallback).
    private static Ticket TicketWithNullIssueType(string id, decimal? storyPoints = null) =>
        new()
        {
            Id = id,
            Summary = id,
            IssueType = null!,
            Priority = "Medium",
            CurrentStatus = "Done",
            StoryPoints = storyPoints
        };

    // A membership whose Ticket reference is null (Ticket is `= null!`); used to pin BR-1's
    // null-Ticket-is-non-bug and BR-23's null-Ticket-groups-as-Unknown behavior.
    private static SprintMembership MembershipWithNullTicket(
        string ticketId, decimal? storyPoints, string finalStatus = "Done") =>
        new()
        {
            TicketId = ticketId,
            FinalStatus = finalStatus,
            StoryPoints = storyPoints,
            Ticket = null!,
            AddedAt = new DateTime(2024, 1, 1)
        };

    private static SprintMembership Membership(
        Ticket ticket,
        decimal? storyPoints,
        string finalStatus = "Done",
        DateTime? removedAt = null) =>
        new()
        {
            TicketId = ticket.Id,
            FinalStatus = finalStatus,
            StoryPoints = storyPoints,
            RemovedAt = removedAt,
            Ticket = ticket,
            AddedAt = new DateTime(2024, 1, 1)
        };

    private static Sprint Sprint(int id, DateTime start, DateTime end, params SprintMembership[] memberships)
    {
        var s = new Sprint { Id = id, Name = $"S{id}", BoardName = "B", StartDate = start, EndDate = end };
        foreach (var m in memberships)
            s.AddMembership(m);
        return s;
    }

    /// <summary>A transition To "Done" inside the given window ⇒ ticket completed in that sprint.</summary>
    private static StatusTransition Completed(string ticketId, DateTime at) =>
        new() { TicketId = ticketId, FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = at };

    private static readonly DateTime S1Start = new(2024, 1, 1);
    private static readonly DateTime S1End = new(2024, 1, 14);
    private static readonly DateTime S1Mid = new(2024, 1, 7);

    private static readonly DateTime S2Start = new(2024, 1, 15);
    private static readonly DateTime S2End = new(2024, 1, 28);
    private static readonly DateTime S2Mid = new(2024, 1, 20);

    private static readonly BugRatioAnalyzer Analyzer = new();

    // =====================================================================
    // BR-2 / BR-3: per-sprint ratio = bugSp/completedSp*100 when completedSp > 0, else 0m
    // Pin the boundary completedSp == 0 → 0m, and a known non-zero ratio.
    // =====================================================================

    // 1 bug @ 4 SP, 1 story @ 6 SP ⇒ bugSp=4 nonBug=6 completed=10 ⇒ ratio = 40.0
    [Fact]
    public void TeamRatio_IsBugSpOverCompletedTimes100_When_CompletedPositive()
    {
        var bug = Membership(TicketOf("BUG-1", "Bug"), 4m);
        var story = Membership(TicketOf("STORY-1", "Story"), 6m);
        var sprint = Sprint(1, S1Start, S1End, bug, story);
        var transitions = new List<StatusTransition> { Completed("BUG-1", S1Mid), Completed("STORY-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.BugRatioPercent.Should().Be(40.0m);
        result.TeamMetrics.TotalBugSp.Should().Be(4.0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(6.0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(10.0m);
    }

    // completedSp == 0 (no completed memberships) ⇒ divide-by-zero guard returns 0m, never throws.
    [Fact]
    public void TeamRatio_IsZero_When_NothingCompleted()
    {
        // membership exists but NO transition To Done ⇒ not completed ⇒ completedSp == 0
        var story = Membership(TicketOf("STORY-1", "Story"), 6m);
        var sprint = Sprint(1, S1Start, S1End, story);

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), statusTransitions: [], subTeam: null);

        result.TeamMetrics.BugRatioPercent.Should().Be(0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(0m);
    }

    // All-bug completed scope ⇒ ratio is exactly 100 (pins the *100 multiplier and bugSp numerator).
    [Fact]
    public void TeamRatio_Is100_When_AllCompletedAreBugs()
    {
        var bug = Membership(TicketOf("BUG-1", "Bug"), 5m);
        var sprint = Sprint(1, S1Start, S1End, bug);
        var transitions = new List<StatusTransition> { Completed("BUG-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.BugRatioPercent.Should().Be(100.0m);
    }

    // No-bug completed scope ⇒ ratio is exactly 0 (pins that non-bugs do NOT count toward bugSp).
    [Fact]
    public void TeamRatio_IsZero_When_NoBugsButWorkCompleted()
    {
        var story = Membership(TicketOf("STORY-1", "Story"), 6m);
        var sprint = Sprint(1, S1Start, S1End, story);
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.BugRatioPercent.Should().Be(0m);
        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(6.0m);
    }

    // =====================================================================
    // BR-1: IsBug iff Ticket.IssueType == "Bug" (case-SENSITIVE ordinal); null Ticket → not a bug.
    // =====================================================================

    // "bug" (lowercase) must NOT classify as a bug — kills an ordinal→ignore-case mutant.
    [Fact]
    public void IssueType_IsCaseSensitive_LowercaseBug_IsNotABug()
    {
        var lower = Membership(TicketOf("T-1", "bug"), 5m);
        var sprint = Sprint(1, S1Start, S1End, lower);
        var transitions = new List<StatusTransition> { Completed("T-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        // Treated as non-bug ⇒ bugSp stays 0, ratio 0.
        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
        result.TeamMetrics.BugRatioPercent.Should().Be(0m);
    }

    // "Bug " (trailing space) must NOT match — pins the exact-string-equality boundary (kills
    // an == → StartsWith/Contains-style mutant and confirms no trimming).
    [Fact]
    public void IssueType_TrailingSpace_IsNotABug()
    {
        var spaced = Membership(TicketOf("T-1", "Bug "), 5m);
        var sprint = Sprint(1, S1Start, S1End, spaced);
        var transitions = new List<StatusTransition> { Completed("T-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
    }

    // Exact "Bug" classifies as bug — the positive half of the == "Bug" boundary.
    [Fact]
    public void IssueType_ExactBug_IsClassifiedAsBug()
    {
        var bug = Membership(TicketOf("T-1", "Bug"), 5m);
        var sprint = Sprint(1, S1Start, S1End, bug);
        var transitions = new List<StatusTransition> { Completed("T-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalBugSp.Should().Be(5.0m);
        result.TeamMetrics.BugRatioPercent.Should().Be(100.0m);
    }

    // BR-1 + BR-15: a completed membership whose Ticket is NULL is non-bug (null?.IssueType != "Bug")
    // and lands in nonBugSp — pins the null-propagation half of IsBug.
    [Fact]
    public void NullTicketMembership_IsNonBug_AndCountsTowardNonBugSp()
    {
        var nullTicket = MembershipWithNullTicket("NT-1", 5m);
        var sprint = Sprint(1, S1Start, S1End, nullTicket);
        var transitions = new List<StatusTransition> { Completed("NT-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
    }

    // =====================================================================
    // BR-10 / BR-11: bug / non-bug ticket counts are complementary Count(IsBug)/Count(!IsBug),
    // emitted as raw integers (unrounded).
    // =====================================================================

    [Fact]
    public void DeveloperCounts_PartitionCompletedSetByIsBug()
    {
        var dev = Dev("d1");
        var bug = Membership(TicketOf("BUG-1", "Bug", assigneeId: "d1"), 3m);
        var s1 = Membership(TicketOf("STORY-1", "Story", assigneeId: "d1"), 2m);
        var s2 = Membership(TicketOf("STORY-2", "Story", assigneeId: "d1"), 2m);
        var sprint = Sprint(1, S1Start, S1End, bug, s1, s2);
        var transitions = new List<StatusTransition>
        {
            Completed("BUG-1", S1Mid), Completed("STORY-1", S1Mid), Completed("STORY-2", S1Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(), transitions, subTeam: null);

        var entry = result.Developers.Single();
        entry.BugTicketCount.Should().Be(1);
        entry.NonBugTicketCount.Should().Be(2);
    }

    // =====================================================================
    // BR-12 / BR-13: completed set EXCLUDES RemovedAt != null and FinalStatus in excludedStatuses
    //      (case-INSENSITIVE OrdinalIgnoreCase). Inclusion needs IsCompletedInSprint == true.
    // =====================================================================

    // RemovedAt != null ⇒ excluded even though a completion transition exists.
    [Fact]
    public void Removed_Membership_IsExcludedFromCompleted()
    {
        var removed = Membership(TicketOf("BUG-1", "Bug"), 5m, removedAt: S1Mid);
        var kept = Membership(TicketOf("STORY-1", "Story"), 5m);
        var sprint = Sprint(1, S1Start, S1End, removed, kept);
        var transitions = new List<StatusTransition> { Completed("BUG-1", S1Mid), Completed("STORY-1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        // Removed bug dropped ⇒ only the story counts.
        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // FinalStatus listed in excludedStatuses ⇒ excluded, matched case-INSENSITIVELY.
    [Fact]
    public void ExcludedFinalStatus_IsMatchedCaseInsensitively()
    {
        // membership FinalStatus = "WON'T DO" but excluded list has different casing
        var excluded = Membership(TicketOf("STORY-1", "Story"), 5m, finalStatus: "WON'T DO");
        var sprint = Sprint(1, S1Start, S1End, excluded);
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1Mid) };
        var settings = Settings(excludedStatuses: ["won't do"]);

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], settings, transitions, subTeam: null);

        // Case-insensitive match drops it ⇒ nothing completed.
        result.TeamMetrics.TotalCompletedSp.Should().Be(0m);
    }

    // Control: same status NOT in the excluded list ⇒ kept. Distinguishes the filter from a no-op.
    [Fact]
    public void NonExcludedFinalStatus_IsKept()
    {
        var kept = Membership(TicketOf("STORY-1", "Story"), 5m, finalStatus: "Done");
        var sprint = Sprint(1, S1Start, S1End, kept);
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1Mid) };
        var settings = Settings(excludedStatuses: ["Won't Do"]);

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], settings, transitions, subTeam: null);

        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // Inclusion requires IsCompletedInSprint == true: a transition OUTSIDE the window ⇒ not completed.
    [Fact]
    public void Completion_OutsideSprintWindow_IsNotCompleted()
    {
        var story = Membership(TicketOf("STORY-1", "Story"), 5m);
        var sprint = Sprint(1, S1Start, S1End, story);
        // transition stamped AFTER S1End ⇒ not in [start,end] ⇒ not completed in this sprint
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1End.AddDays(1)) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalCompletedSp.Should().Be(0m);
    }

    // BR-28 boundary: a transition exactly AT sprintEnd is inside the inclusive [start, end] window.
    [Fact]
    public void Completion_ExactlyAtSprintEnd_IsCompleted()
    {
        var story = Membership(TicketOf("STORY-1", "Story"), 5m);
        var sprint = Sprint(1, S1Start, S1End, story);
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1End) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // BR-28 boundary: a transition exactly AT sprintStart is inside the inclusive window.
    [Fact]
    public void Completion_ExactlyAtSprintStart_IsCompleted()
    {
        var story = Membership(TicketOf("STORY-1", "Story"), 5m);
        var sprint = Sprint(1, S1Start, S1End, story);
        var transitions = new List<StatusTransition> { Completed("STORY-1", S1Start) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // =====================================================================
    // BR-5: aggregate ratio is ratio-of-totals (ΣbugSp / ΣcompletedSp × 100), NOT mean of per-sprint ratios.
    // S1: bug 30, non 10 (total 40) ratio 75. S2: bug 5, non 45 (total 50) ratio 10.
    // mean = 42.5 ; ratio-of-totals = (30+5)/(40+50)*100 = 35/90*100 = 38.8889 → round 38.9.
    // =====================================================================

    [Fact]
    public void TeamAggregate_IsRatioOfTotals_NotMeanOfPerSprintRatios()
    {
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 30m),
            Membership(TicketOf("S1", "Story"), 10m));
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B2", "Bug"), 5m),
            Membership(TicketOf("S2", "Story"), 45m));
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid),
            Completed("B2", S2Mid), Completed("S2", S2Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [s1, s2], [s1, s2], [], Settings(), transitions, subTeam: null);

        // ratio-of-totals = 35 / 90 * 100 = 38.8889 → Round(.,1) = 38.9
        result.TeamMetrics.BugRatioPercent.Should().Be(38.9m);
        // mean-of-ratios (the WRONG answer) would be (75 + 10) / 2 = 42.5 — explicitly excluded.
        result.TeamMetrics.BugRatioPercent.Should().NotBe(42.5m);
    }

    // =====================================================================
    // BR-21 / BR-22: alert streak. Count most-recent-first while ratio >= threshold (INCLUSIVE);
    // fire when streak >= consecutiveCount.
    // =====================================================================

    // Two consecutive sprints both exactly AT threshold (ratio == threshold) ⇒ streak 2, fires at count 2.
    // ratio == threshold must COUNT (>= is inclusive) — kills a >= → > mutant.
    [Fact]
    public void Alert_Fires_When_RatioExactlyAtThreshold_ForConsecutiveCount()
    {
        var dev = Dev("d1");
        // bug 5, non-bug 5 ⇒ ratio = 50 exactly. threshold = 50.
        Sprint MkSprint(int id, DateTime start, DateTime end) => Sprint(id, start, end,
            Membership(TicketOf($"B{id}", "Bug", assigneeId: "d1"), 5m),
            Membership(TicketOf($"S{id}", "Story", assigneeId: "d1"), 5m));
        var s1 = MkSprint(1, S1Start, S1End);
        var s2 = MkSprint(2, S2Start, S2End);
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid),
            Completed("B2", S2Mid), Completed("S2", S2Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [s1], [s1, s2], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 2), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        alert.ConsecutiveSprintCount.Should().Be(2);
        alert.IsActive.Should().BeTrue();
        alert.ThresholdPercent.Should().Be(50);
    }

    // ratio JUST BELOW threshold ⇒ streak breaks immediately ⇒ does NOT fire.
    // Pins the strict side of >= (a < value is excluded).
    [Fact]
    public void Alert_DoesNotFire_When_RatioJustBelowThreshold()
    {
        var dev = Dev("d1");
        // bug 4, non-bug 6 ⇒ ratio = 40 < 50 ⇒ no streak.
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 4m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 6m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 1), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        alert.ConsecutiveSprintCount.Should().Be(0);
        alert.IsActive.Should().BeFalse();
    }

    // streak == consecutiveCount-1 ⇒ does NOT fire; streak == consecutiveCount ⇒ fires.
    // Pins the >= on (consecutiveAbove >= consecutiveCount): exactly one above-threshold sprint, need 2.
    [Fact]
    public void Alert_DoesNotFire_When_StreakOneBelowRequiredCount()
    {
        var dev = Dev("d1");
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 8m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 2m)); // ratio 80 >= 50
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 2), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        alert.ConsecutiveSprintCount.Should().Be(1);
        alert.IsActive.Should().BeFalse(); // 1 >= 2 is false
    }

    // streak exactly == consecutiveCount ⇒ fires (1 sprint, count 1). Pins the inclusive == on the fire test.
    [Fact]
    public void Alert_Fires_When_StreakExactlyEqualsRequiredCount()
    {
        var dev = Dev("d1");
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 8m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 2m)); // ratio 80 >= 50
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 1), transitions, subTeam: null);

        result.Developers.Single().Alert.IsActive.Should().BeTrue();
    }

    // =====================================================================
    // BR-20: a sprint with completedSp == 0 BREAKS the streak (most-recent-first), not skipped.
    // Most recent sprint has zero completed ⇒ streak stops at 0 even though older sprints are above threshold.
    // =====================================================================

    [Fact]
    public void Alert_Streak_BreaksAtZeroSpSprint_MostRecentFirst()
    {
        var dev = Dev("d1");
        // Older sprint (S1): above threshold. Newer sprint (S2): nothing completed ⇒ zero SP.
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 1m)); // ratio 90
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B2", "Bug", assigneeId: "d1"), 9m)); // present but NOT completed
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid)
            // no transition for B2 ⇒ S2 completedSp == 0
        };

        // allClosedSprints includes both; most-recent-first = S2 (zero) then S1.
        var result = Analyzer.ComputeMultiSprint(
            [s1], [s1, s2], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 1), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        // S2 (most recent) has zero SP ⇒ break BEFORE counting S1 ⇒ streak 0.
        alert.ConsecutiveSprintCount.Should().Be(0);
        alert.IsActive.Should().BeFalse();
    }

    // Control for the break: when the most-recent sprint DOES complete work above threshold, the streak counts it.
    // Distinguishes "break on zero" from "always zero".
    [Fact]
    public void Alert_Streak_CountsMostRecentSprint_When_ItHasCompletedWork()
    {
        var dev = Dev("d1");
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 1m)); // ratio 90
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B2", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("S2", "Story", assigneeId: "d1"), 1m)); // ratio 90, completed
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid),
            Completed("B2", S2Mid), Completed("S2", S2Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [s1], [s1, s2], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 2), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        alert.ConsecutiveSprintCount.Should().Be(2);
        alert.IsActive.Should().BeTrue();
    }

    // =====================================================================
    // BR-16 / BR-17 / BR-18: sub-team / assignee filters are case-SENSITIVE ordinal ==;
    //      blank subTeam disables the filter. excludedDeveloperIds drops matching assignees but
    //      KEEPS null-assignee memberships, and applies on the TEAM path only.
    // =====================================================================

    // BR-16: Blank subTeam ⇒ no developer filtering: a developer with a different sub-team is still included.
    [Fact]
    public void BlankSubTeam_DisablesDeveloperFilter()
    {
        var dev = Dev("d1", subTeam: "Alpha");
        var result = Analyzer.ComputeMultiSprint(
            [Sprint(1, S1Start, S1End)], [Sprint(1, S1Start, S1End)], [dev],
            Settings(), statusTransitions: [], subTeam: "");

        result.Developers.Should().HaveCount(1);
    }

    // BR-16: whitespace-only subTeam is also treated as blank (IsNullOrWhiteSpace) ⇒ no filtering.
    [Fact]
    public void WhitespaceSubTeam_DisablesDeveloperFilter()
    {
        var dev = Dev("d1", subTeam: "Alpha");
        var result = Analyzer.ComputeMultiSprint(
            [Sprint(1, S1Start, S1End)], [Sprint(1, S1Start, S1End)], [dev],
            Settings(), statusTransitions: [], subTeam: "   ");

        result.Developers.Should().HaveCount(1);
    }

    // BR-16: Non-blank subTeam filters developers by case-SENSITIVE ordinal equality.
    // "alpha" != "Alpha" ⇒ developer excluded.
    [Fact]
    public void SubTeamFilter_IsCaseSensitive_ForDevelopers()
    {
        var devAlpha = Dev("d1", subTeam: "Alpha");
        var result = Analyzer.ComputeMultiSprint(
            [Sprint(1, S1Start, S1End)], [Sprint(1, S1Start, S1End)], [devAlpha],
            Settings(), statusTransitions: [], subTeam: "alpha");

        result.Developers.Should().BeEmpty();
    }

    // BR-16: Exact sub-team match keeps the developer (positive side of the == filter).
    [Fact]
    public void SubTeamFilter_KeepsExactMatchDeveloper()
    {
        var devAlpha = Dev("d1", subTeam: "Alpha");
        var result = Analyzer.ComputeMultiSprint(
            [Sprint(1, S1Start, S1End)], [Sprint(1, S1Start, S1End)], [devAlpha],
            Settings(), statusTransitions: [], subTeam: "Alpha");

        result.Developers.Should().HaveCount(1);
    }

    // BR-17: excludedDeveloperIds drops the matching assignee on the TEAM path …
    [Fact]
    public void ExcludedDeveloperIds_DropMatchingAssignee_OnTeamPath()
    {
        var excludedBug = Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 5m);
        var keptStory = Membership(TicketOf("S1", "Story", assigneeId: "d2"), 5m);
        var sprint = Sprint(1, S1Start, S1End, excludedBug, keptStory);
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null,
            excludedDeveloperIds: ["d1"]);

        // d1's bug is dropped from the TEAM totals ⇒ only d2's story remains.
        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
    }

    // BR-17: … but KEEPS a null-assignee membership even when excludedDeveloperIds is non-empty.
    [Fact]
    public void ExcludedDeveloperIds_KeepNullAssigneeMembership_OnTeamPath()
    {
        var nullAssignee = Membership(TicketOf("S1", "Story", assigneeId: null), 5m);
        var sprint = Sprint(1, S1Start, S1End, nullAssignee);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null,
            excludedDeveloperIds: ["d1"]);

        // null assignee is NOT dropped by exclusion ⇒ story still counts.
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // =====================================================================
    // BR-23 / BR-24: issue-type breakdown groups by IssueType (null → "Unknown"), ordered by
    //      TicketCount desc.
    // =====================================================================

    [Fact]
    public void IssueTypeBreakdown_GroupsByType_OrderedByCountDescending()
    {
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 1m),
            Membership(TicketOf("S1", "Story"), 1m),
            Membership(TicketOf("S2", "Story"), 1m),
            Membership(TicketOf("S3", "Story"), 1m));
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid), Completed("S2", S1Mid), Completed("S3", S1Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        result.IssueTypeBreakdown.Should().HaveCount(2);
        // Story (3) must precede Bug (1) — descending by TicketCount.
        result.IssueTypeBreakdown[0].IssueType.Should().Be("Story");
        result.IssueTypeBreakdown[0].TicketCount.Should().Be(3);
        result.IssueTypeBreakdown[1].IssueType.Should().Be("Bug");
        result.IssueTypeBreakdown[1].TicketCount.Should().Be(1);
    }

    // BR-23: GroupBy is case-SENSITIVE/ordinal — "Bug" and "bug" form SEPARATE groups.
    [Fact]
    public void IssueTypeBreakdown_IsCaseSensitive_DistinctGroupsForDifferentCase()
    {
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 1m),
            Membership(TicketOf("B2", "bug"), 1m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("B2", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        // Two distinct case-sensitive groups, not one merged group.
        result.IssueTypeBreakdown.Should().HaveCount(2);
        result.IssueTypeBreakdown.Select(e => e.IssueType).Should().Contain(["Bug", "bug"]);
    }

    // =====================================================================
    // BR-4: per-sprint emitted SP/ratio figures are rounded to 1 dp (Math.Round, banker's rounding).
    // bug 1, non-bug 2 ⇒ ratio = 33.3333… → Round(.,1) = 33.3.
    // =====================================================================

    [Fact]
    public void PerSprintRatio_IsRoundedToOneDecimalPlace()
    {
        var sprint = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 1m),
            Membership(TicketOf("S1", "Story"), 2m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        var trend = result.TeamMetrics.PerSprintTrend.Single();
        // 1 / 3 * 100 = 33.3333… rounded to 1 dp.
        trend.BugRatioPercent.Should().Be(33.3m);
        trend.BugSp.Should().Be(1.0m);
        trend.NonBugSp.Should().Be(2.0m);
        trend.CompletedSp.Should().Be(3.0m);
    }

    // =====================================================================
    // BR-15 / GetEffectiveSp: defaultSpPerBug fallback — a Bug with null StoryPoints contributes
    // defaultSpPerBug when defaultSpPerBug > 0, and 0 when defaultSpPerBug == 0.
    // =====================================================================

    [Fact]
    public void BugWithNullStoryPoints_UsesDefaultSpPerBug_When_Positive()
    {
        var bug = Membership(TicketOf("B1", "Bug"), storyPoints: null);
        var sprint = Sprint(1, S1Start, S1End, bug);
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(defaultSpPerBug: 3), transitions, subTeam: null);

        result.TeamMetrics.TotalBugSp.Should().Be(3.0m);
    }

    [Fact]
    public void BugWithNullStoryPoints_ContributesZero_When_DefaultSpPerBugIsZero()
    {
        var bug = Membership(TicketOf("B1", "Bug"), storyPoints: null);
        var sprint = Sprint(1, S1Start, S1End, bug);
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(defaultSpPerBug: 0), transitions, subTeam: null);

        // null effective SP ⇒ treated as 0 ⇒ no completed SP at all.
        result.TeamMetrics.TotalBugSp.Should().Be(0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(0m);
    }

    // =====================================================================
    // BR-30 / BR-34 (single-sprint): prior-sprint deltas are emitted ONLY when a prior sprint exists.
    // =====================================================================

    [Fact]
    public void SingleSprint_DeveloperDelta_IsNull_When_NoPriorSprint()
    {
        var dev = Dev("d1");
        var target = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 5m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid) };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: null, [target], [dev], Settings(), transitions, subTeam: null);

        result.Developers.Single().Delta.Should().BeNull();
    }

    [Fact]
    public void SingleSprint_DeveloperDelta_IsEmitted_When_PriorSprintExists()
    {
        var dev = Dev("d1");
        var prior = Sprint(1, S1Start, S1End,
            Membership(TicketOf("PB1", "Bug", assigneeId: "d1"), 2m));
        var target = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 5m));
        var transitions = new List<StatusTransition>
        {
            Completed("PB1", S1Mid), Completed("B1", S2Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: prior, [prior, target], [dev], Settings(), transitions, subTeam: null);

        var delta = result.Developers.Single().Delta;
        delta.Should().NotBeNull();
        // current bug SP 5 − prior bug SP 2 = 3
        delta!.BugSpDelta.Should().Be(3.0m);
        delta.BugSpDeltaDirection.Should().Be("up");
    }

    // =====================================================================
    // BR-31 polarity wiring on the single-sprint TEAM cards:
    // Bug Ratio % and Bug SP = "positive-down"; Non-Bug SP = "positive-up".
    // A RISE in Bug SP (delta > 0) under positive-down ⇒ polarity "negative".
    // A RISE in Non-Bug SP under positive-up ⇒ polarity "positive".
    // =====================================================================

    [Fact]
    public void SingleSprint_TeamCardPolarity_BugSpRise_IsNegative_NonBugSpRise_IsPositive()
    {
        var prior = Sprint(1, S1Start, S1End,
            Membership(TicketOf("PB1", "Bug"), 2m),
            Membership(TicketOf("PS1", "Story"), 2m));
        var target = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B1", "Bug"), 5m),     // bug SP rose 2 → 5
            Membership(TicketOf("S1", "Story"), 5m));  // non-bug SP rose 2 → 5
        var transitions = new List<StatusTransition>
        {
            Completed("PB1", S1Mid), Completed("PS1", S1Mid),
            Completed("B1", S2Mid), Completed("S1", S2Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: prior, [prior, target], [], Settings(), transitions, subTeam: null);

        // Bug SP card: rise under positive-down ⇒ "negative".
        result.TeamMetrics.BugSp.DeltaPolarity.Should().Be("negative");
        // Non-Bug SP card: rise under positive-up ⇒ "positive".
        result.TeamMetrics.NonBugSp.DeltaPolarity.Should().Be("positive");
    }

    // =====================================================================
    // MUTANT-TARGETED SUPPLEMENT
    // Each [Fact] below pins a behavior that the boundary tests above leave
    // ambiguous, killing a specific surviving mutant. Comments cite the
    // production line + the mutation each test exists to kill.
    // =====================================================================

    // ------------------------------------------------------------------
    // BR-7 / L20: sortedTarget = targetSprints.OrderBy(s => s.StartDate)
    //   Mutant: OrderBy → OrderByDescending. The per-sprint trend (and the
    //   pooled issue-type breakdown) follow sortedTarget order. Feed sprints
    //   reversed and pin that the trend comes back ASCENDING by StartDate.
    // ------------------------------------------------------------------
    [Fact]
    public void PerSprintTrend_IsOrderedAscendingByStartDate_EvenWhenInputReversed()
    {
        // S1 (Jan) has bug 1 / story 9 ⇒ ratio 10. S2 (later) has bug 9 / story 1 ⇒ ratio 90.
        // Distinct ratios let us read the order off the trend rows.
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 1m),
            Membership(TicketOf("S1", "Story"), 9m));
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B2", "Bug"), 9m),
            Membership(TicketOf("S2", "Story"), 1m));
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid),
            Completed("B2", S2Mid), Completed("S2", S2Mid)
        };

        // Pass the LATER sprint first; OrderBy(StartDate) must reorder so the
        // earlier-starting sprint's row is index 0. OrderByDescending would flip it.
        var result = Analyzer.ComputeMultiSprint(
            [s2, s1], [s1, s2], [], Settings(), transitions, subTeam: null);

        var trend = result.TeamMetrics.PerSprintTrend;
        trend.Should().HaveCount(2);
        trend[0].SprintId.Should().Be(1);          // earliest StartDate first
        trend[0].BugRatioPercent.Should().Be(10.0m);
        trend[1].SprintId.Should().Be(2);          // latest StartDate last
        trend[1].BugRatioPercent.Should().Be(90.0m);
    }

    // ------------------------------------------------------------------
    // BR-8 / L23-25: sprintInfos copy Id/Name/StartDate/EndDate verbatim from the
    //   StartDate-ascending sorted target list. Pin field-by-field so a swapped
    //   field or a wrong source mutant dies, and confirm ordering on Sprints too.
    // ------------------------------------------------------------------
    [Fact]
    public void SprintInfos_CopyFieldsVerbatim_InAscendingStartDateOrder()
    {
        var s1 = Sprint(7, S1Start, S1End);
        var s2 = Sprint(9, S2Start, S2End);

        var result = Analyzer.ComputeMultiSprint(
            [s2, s1], [s1, s2], [], Settings(), statusTransitions: [], subTeam: null);

        result.Sprints.Should().HaveCount(2);
        result.Sprints[0].Id.Should().Be(7);
        result.Sprints[0].Name.Should().Be("S7");
        result.Sprints[0].StartDate.Should().Be(S1Start);
        result.Sprints[0].EndDate.Should().Be(S1End);
        result.Sprints[1].Id.Should().Be(9);
        result.Sprints[1].StartDate.Should().Be(S2Start);
    }

    // ------------------------------------------------------------------
    // BR-9 / L92/93/96/97: dev aggregates are Sum over sprintBreakdowns
    //   devBugSp = Σ BugSp, devNonBugSp = Σ NonBugSp,
    //   devBugCount = Σ BugTicketCount, devNonBugCount = Σ NonBugTicketCount.
    //   Mutant: Sum → Max. With ONE breakdown Sum==Max; we need a developer
    //   active in TWO target sprints with unequal positive per-sprint values,
    //   so Σ strictly exceeds Max and the mutant is killed.
    // L94: devCompletedSp = devBugSp + devNonBugSp (arithmetic '+' not '-').
    // ------------------------------------------------------------------
    [Fact]
    public void MultiSprint_DeveloperAggregates_AreSumAcrossSprintBreakdowns_NotMax()
    {
        var dev = Dev("d1");
        // S1: 2 bugs (3 SP total) + 1 story (4 SP). S2: 1 bug (5 SP) + 2 stories (6 SP total).
        // Σ bugSp = 3 + 5 = 8 (Max row = 5). Σ nonBugSp = 4 + 6 = 10 (Max row = 6).
        // Σ bugCount = 2 + 1 = 3 (Max row = 2). Σ nonBugCount = 1 + 2 = 3 (Max row = 2).
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 1m),
            Membership(TicketOf("B2", "Bug", assigneeId: "d1"), 2m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 4m));
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B3", "Bug", assigneeId: "d1"), 5m),
            Membership(TicketOf("S2", "Story", assigneeId: "d1"), 2m),
            Membership(TicketOf("S3", "Story", assigneeId: "d1"), 4m));
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("B2", S1Mid), Completed("S1", S1Mid),
            Completed("B3", S2Mid), Completed("S2", S2Mid), Completed("S3", S2Mid)
        };

        var result = Analyzer.ComputeMultiSprint(
            [s1, s2], [s1, s2], [dev], Settings(), transitions, subTeam: null);

        var entry = result.Developers.Single();
        entry.BugSp.Should().Be(8.0m);            // Σ (kills Sum→Max: Max would be 5)
        entry.NonBugSp.Should().Be(10.0m);        // Σ (kills Sum→Max: Max would be 6)
        entry.CompletedSp.Should().Be(18.0m);     // L94: 8 + 10 (kills '+'→'-': would be -2)
        entry.BugTicketCount.Should().Be(3);      // Σ (kills Sum→Max: Max would be 2)
        entry.NonBugTicketCount.Should().Be(3);   // Σ (kills Sum→Max: Max would be 2)
    }

    // ------------------------------------------------------------------
    // BR-9 / L95: devRatio = devBugSp / devCompletedSp * 100 when devCompletedSp > 0.
    //   Kills: '/'→'*', '*'→'/', the false-conditional (→ 0m), and the
    //   completedSp comparison mutants — by pinning an exact non-round ratio.
    //   8 / 18 * 100 = 44.4444… → Round(.,1) = 44.4.
    // ------------------------------------------------------------------
    [Fact]
    public void MultiSprint_DeveloperRatio_IsBugSpOverCompletedTimes100()
    {
        var dev = Dev("d1");
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 8m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 10m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [s1], [s1], [dev], Settings(), transitions, subTeam: null);

        // 8 / 18 * 100 = 44.4444… → 44.4
        result.Developers.Single().BugRatioPercent.Should().Be(44.4m);
    }

    // ------------------------------------------------------------------
    // BR-37 / L104-113: developer output record carries identity fields verbatim
    //   (Id/DisplayName/SubTeam/AvatarUrl). Pin so a swapped/blanked identity field dies.
    // ------------------------------------------------------------------
    [Fact]
    public void MultiSprint_DeveloperRecord_CarriesIdentityFields()
    {
        var dev = new Developer { Id = "acc-1", DisplayName = "Ada", SubTeam = "Alpha", AvatarUrl = "http://a/x.png" };
        var result = Analyzer.ComputeMultiSprint(
            [Sprint(1, S1Start, S1End)], [Sprint(1, S1Start, S1End)], [dev],
            Settings(), statusTransitions: [], subTeam: null);

        var entry = result.Developers.Single();
        entry.AccountId.Should().Be("acc-1");
        entry.DisplayName.Should().Be("Ada");
        entry.SubTeam.Should().Be("Alpha");
        entry.AvatarUrl.Should().Be("http://a/x.png");
    }

    // ------------------------------------------------------------------
    // BR-29 / L144/145: single-sprint TEAM metrics.
    //   completedSp = bugSp + nonBugSp (L144 '+'); ratio = bugSp/completedSp*100
    //   when completedSp > 0 (L145). The boundary suite only checked single-sprint
    //   POLARITY — here we pin the raw card .Value numbers so the arithmetic and
    //   conditional mutants on L144/L145 die.
    //   bug 3 / story 9 ⇒ completed 12, ratio 25.
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_TeamCards_CarryRawRatioAndSpValues()
    {
        var target = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 3m),
            Membership(TicketOf("S1", "Story"), 9m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: null, [target], [], Settings(), transitions, subTeam: null);

        // L145: 3 / 12 * 100 = 25 (kills false-conditional→0m, '/'→'*', '*'→'/').
        result.TeamMetrics.BugRatioPercent.Value.Should().Be(25m);
        result.TeamMetrics.BugSp.Value.Should().Be(3m);
        // L144: completedSp feeds nothing the card stores directly, but nonBugSp does.
        result.TeamMetrics.NonBugSp.Value.Should().Be(9m);
    }

    // ------------------------------------------------------------------
    // BR-32 / L165/167/169: the team card DisplayValue strings are interpolated
    //   ($"{ratio:0.#}%", $"{bugSp:0.#}", $"{nonBugSp:0.#}").
    //   Mutant: replace the interpolated string with "" or $"". Pin the exact
    //   rendered text so an emptied/blanked literal is caught, and the card Names.
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_TeamCard_DisplayValues_AreInterpolatedNotBlank()
    {
        var target = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 3m),
            Membership(TicketOf("S1", "Story"), 9m));
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: null, [target], [], Settings(), transitions, subTeam: null);

        // ratio 25 ⇒ "25%"; bugSp 3 ⇒ "3"; nonBugSp 9 ⇒ "9".
        result.TeamMetrics.BugRatioPercent.DisplayValue.Should().Be("25%");
        result.TeamMetrics.BugSp.DisplayValue.Should().Be("3");
        result.TeamMetrics.NonBugSp.DisplayValue.Should().Be("9");
        result.TeamMetrics.BugRatioPercent.Name.Should().Be("Bug Ratio %");
        result.TeamMetrics.BugSp.Name.Should().Be("Bug SP");
        result.TeamMetrics.NonBugSp.Name.Should().Be("Non-Bug SP");
    }

    // ------------------------------------------------------------------
    // BR-32 fractional: the "0.#" format keeps ONE decimal and drops a trailing zero.
    //   ratio 33.3333 → "33.3%" (one decimal kept); a whole 5 → "5" (zero dropped).
    //   Kills a format-string literal mutant ("0.#" → "0" or "0.0").
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_TeamCard_DisplayValue_UsesOneDecimalFormat()
    {
        var target = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug"), 1m),
            Membership(TicketOf("S1", "Story"), 2m));   // ratio 1/3*100 = 33.333…
        var transitions = new List<StatusTransition> { Completed("B1", S1Mid), Completed("S1", S1Mid) };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: null, [target], [], Settings(), transitions, subTeam: null);

        // "0.#" ⇒ at most one decimal ⇒ "33.3%".
        result.TeamMetrics.BugRatioPercent.DisplayValue.Should().Be("33.3%");
        // bugSp 1 ⇒ no fractional part ⇒ "1" (trailing-zero decimal dropped).
        result.TeamMetrics.BugSp.DisplayValue.Should().Be("1");
    }

    // ------------------------------------------------------------------
    // BR-30 / BR-31 / L160/161/166/168/170: prior-sprint TEAM metrics
    //   (priorCompletedSp = pBugSp + pNonBugSp; priorRatio = priorBugSp/priorCompletedSp*100 when > 0).
    //   team-card deltas = current − prior (ratio/bugSp/nonBugSp).
    //   Mutant set: '+'→'-' / false-conditional / '−'→'+' on each delta, plus the
    //   prior-ratio conditional. Pin every team delta value explicitly.
    //   Prior: bug 2 / story 8 (ratio 20). Current: bug 6 / story 6 (ratio 50).
    //   Δratio = 50 − 20 = 30; ΔbugSp = 6 − 2 = 4; ΔnonBugSp = 6 − 8 = −2.
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_TeamCardDeltas_AreCurrentMinusPrior()
    {
        var prior = Sprint(1, S1Start, S1End,
            Membership(TicketOf("PB1", "Bug"), 2m),
            Membership(TicketOf("PS1", "Story"), 8m));
        var target = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B1", "Bug"), 6m),
            Membership(TicketOf("S1", "Story"), 6m));
        var transitions = new List<StatusTransition>
        {
            Completed("PB1", S1Mid), Completed("PS1", S1Mid),
            Completed("B1", S2Mid), Completed("S1", S2Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: prior, [prior, target], [], Settings(), transitions, subTeam: null);

        // Δratio = 50 − 20 = 30 (kills L166 '−'→'+' [would be 70] and false-conditional [null]).
        result.TeamMetrics.BugRatioPercent.Delta.Should().Be(30m);
        // ΔbugSp = 6 − 2 = 4 (kills L168 '−'→'+' [would be 8]).
        result.TeamMetrics.BugSp.Delta.Should().Be(4m);
        // ΔnonBugSp = 6 − 8 = −2 (kills L170 '−'→'+' [would be 14]).
        result.TeamMetrics.NonBugSp.Delta.Should().Be(-2m);
        // Directions corroborate the signs.
        result.TeamMetrics.BugRatioPercent.DeltaDirection.Should().Be("up");
        result.TeamMetrics.NonBugSp.DeltaDirection.Should().Be("down");
    }

    // ------------------------------------------------------------------
    // BR-33 / L185/186/191: single-sprint per-DEVELOPER SP + ratio.
    //   devCompletedSp = devBugSp + devNonBugSp (L185 '+'); devRatio (L186).
    //   devNonBugCount = devCompleted.Count(m => !IsBug(m)) (L191).
    //   Mutant: !IsBug → IsBug. Pin a mixed bug/non-bug developer so non-bug
    //   count ≠ bug count and the un-negated predicate is caught.
    //   bug 4 / story 12 ⇒ completed 16, ratio 25; 1 bug + 2 stories.
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_DeveloperSpRatioAndNonBugCount_ArePinned()
    {
        var dev = Dev("d1");
        var target = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 4m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 7m),
            Membership(TicketOf("S2", "Story", assigneeId: "d1"), 5m));
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid), Completed("S2", S1Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: null, [target], [dev], Settings(), transitions, subTeam: null);

        var entry = result.Developers.Single();
        entry.BugSp.Should().Be(4m);
        entry.NonBugSp.Should().Be(12m);
        entry.CompletedSp.Should().Be(16m);            // L185: 4 + 12 (kills '+'→'-')
        entry.BugRatioPercent.Should().Be(25m);        // L186: 4/16*100 = 25
        entry.BugTicketCount.Should().Be(1);
        entry.NonBugTicketCount.Should().Be(2);        // L191: !IsBug (kills !IsBug→IsBug, which would give 1)
    }

    // ------------------------------------------------------------------
    // BR-34 / BR-35 / L200/201/206/208-219: single-sprint per-DEVELOPER delta bundle.
    //   priorDevCompletedSp = priorDevBugSp + priorDevNonBugSp (L200);
    //   priorDevRatio (L201); priorDevNonBugCount = Count(!IsBug) (L206);
    //   the 5 deltas (L208-212) = current − prior; the 5 Direction calls and
    //   the 5 Polarity calls with their EXACT positiveUp flags (L215-219).
    //   Boolean mutants flip positiveUp on each Polarity; pin every polarity so
    //   each flip is caught.
    //
    //   Prior dev: bug 2 / 2 stories @ 4 each = 8 ⇒ bugSp 2, nonBugSp 8, ratio 20,
    //              bugCount 1, nonBugCount 2.
    //   Current dev: bug 6 / 1 story 2          ⇒ bugSp 6, nonBugSp 2, ratio 75,
    //              bugCount 1, nonBugCount 1.
    //   Deltas: ΔbugSp = 4 (>0), ΔnonBugSp = −6 (<0), Δratio = 55 (>0),
    //           ΔbugCount = 0, ΔnonBugCount = −1 (<0).
    //   Polarity flags (from L215-219):
    //     bugSp positiveUp:false  ⇒ Δ>0 ⇒ "negative"
    //     nonBugSp positiveUp:true ⇒ Δ<0 ⇒ "negative"
    //     ratio positiveUp:false  ⇒ Δ>0 ⇒ "negative"
    //     bugCount positiveUp:false ⇒ Δ==0 ⇒ "neutral"
    //     nonBugCount positiveUp:true ⇒ Δ<0 ⇒ "negative"
    // ------------------------------------------------------------------
    [Fact]
    public void SingleSprint_DeveloperDeltaBundle_PinsEveryDeltaDirectionAndPolarity()
    {
        var dev = Dev("d1");
        var prior = Sprint(1, S1Start, S1End,
            Membership(TicketOf("PB1", "Bug", assigneeId: "d1"), 2m),
            Membership(TicketOf("PS1", "Story", assigneeId: "d1"), 4m),
            Membership(TicketOf("PS2", "Story", assigneeId: "d1"), 4m));
        var target = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 6m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 2m));
        var transitions = new List<StatusTransition>
        {
            Completed("PB1", S1Mid), Completed("PS1", S1Mid), Completed("PS2", S1Mid),
            Completed("B1", S2Mid), Completed("S1", S2Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: prior, [prior, target], [dev], Settings(), transitions, subTeam: null);

        var delta = result.Developers.Single().Delta;
        delta.Should().NotBeNull();

        // L208 ΔbugSp = 6 − 2 = 4; L215 Direction "up", Polarity positiveUp:false ⇒ "negative".
        delta!.BugSpDelta.Should().Be(4m);
        delta.BugSpDeltaDirection.Should().Be("up");
        delta.BugSpDeltaPolarity.Should().Be("negative");

        // L209 ΔnonBugSp = 2 − 8 = −6; L216 Direction "down", Polarity positiveUp:true ⇒ "negative".
        delta.NonBugSpDelta.Should().Be(-6m);
        delta.NonBugSpDeltaDirection.Should().Be("down");
        delta.NonBugSpDeltaPolarity.Should().Be("negative");

        // L210 Δratio = 75 − 20 = 55; L217 Direction "up", Polarity positiveUp:false ⇒ "negative".
        delta.BugRatioPercentDelta.Should().Be(55m);
        delta.BugRatioPercentDeltaDirection.Should().Be("up");
        delta.BugRatioPercentDeltaPolarity.Should().Be("negative");

        // L211 ΔbugCount = 1 − 1 = 0; L218 Direction "flat", Polarity ⇒ "neutral".
        delta.BugTicketCountDelta.Should().Be(0);
        delta.BugTicketCountDeltaDirection.Should().Be("flat");
        delta.BugTicketCountDeltaPolarity.Should().Be("neutral");

        // L206/L212 ΔnonBugCount = 1 − 2 = −1; L219 Direction "down", Polarity positiveUp:true ⇒ "negative".
        delta.NonBugTicketCountDelta.Should().Be(-1);
        delta.NonBugTicketCountDeltaDirection.Should().Be("down");
        delta.NonBugTicketCountDeltaPolarity.Should().Be("negative");
    }

    // Control for the L215-219 boolean polarity flips: a FALL in bug SP (Δ<0)
    // under positive-down ⇒ "positive"; a RISE in non-bug SP (Δ>0) under
    // positive-up ⇒ "positive". Together with the test above (opposite signs),
    // each positiveUp flag is observed on BOTH sides, so flipping any boolean
    // changes at least one asserted polarity.
    [Fact]
    public void SingleSprint_DeveloperDeltaBundle_BugSpFall_IsPositive_NonBugSpRise_IsPositive()
    {
        var dev = Dev("d1");
        var prior = Sprint(1, S1Start, S1End,
            Membership(TicketOf("PB1", "Bug", assigneeId: "d1"), 8m),
            Membership(TicketOf("PS1", "Story", assigneeId: "d1"), 2m));
        var target = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 3m),   // bug SP fell 8 → 3
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 9m)); // non-bug SP rose 2 → 9
        var transitions = new List<StatusTransition>
        {
            Completed("PB1", S1Mid), Completed("PS1", S1Mid),
            Completed("B1", S2Mid), Completed("S1", S2Mid)
        };

        var result = Analyzer.ComputeSingleSprint(
            target, priorSprint: prior, [prior, target], [dev], Settings(), transitions, subTeam: null);

        var delta = result.Developers.Single().Delta;
        delta.Should().NotBeNull();
        // ΔbugSp = 3 − 8 = −5 < 0; positive-down ⇒ "positive".
        delta!.BugSpDelta.Should().Be(-5m);
        delta.BugSpDeltaPolarity.Should().Be("positive");
        // ΔnonBugSp = 9 − 2 = 7 > 0; positive-up ⇒ "positive".
        delta.NonBugSpDelta.Should().Be(7m);
        delta.NonBugSpDeltaPolarity.Should().Be("positive");
        // Δratio: prior 80, current 25 ⇒ −55 < 0; positive-down ⇒ "positive".
        delta.BugRatioPercentDeltaPolarity.Should().Be("positive");
    }

    // ------------------------------------------------------------------
    // BR-21 / L278: the streak loop's else branch is `break;`.
    //   Mutant: break → ; (empty statement) — the loop would CONTINUE past a
    //   below-threshold sprint instead of stopping. Arrange most-recent-first:
    //   newest above, MIDDLE below threshold, oldest above. A correct break
    //   stops at the middle ⇒ streak = 1. The mutant keeps going and counts the
    //   oldest too ⇒ streak = 2. Pin streak == 1.
    // ------------------------------------------------------------------
    [Fact]
    public void Alert_Streak_StopsAtFirstBelowThresholdSprint_DoesNotResume()
    {
        var dev = Dev("d1");
        var sNewest = new DateTime(2024, 3, 1);
        var sNewestEnd = new DateTime(2024, 3, 14);
        var sMid = new DateTime(2024, 2, 1);
        var sMidEnd = new DateTime(2024, 2, 14);
        var sOldest = new DateTime(2024, 1, 1);
        var sOldestEnd = new DateTime(2024, 1, 14);

        // Newest: ratio 90 (above). Middle: ratio 10 (below). Oldest: ratio 90 (above).
        var newest = Sprint(3, sNewest, sNewestEnd,
            Membership(TicketOf("NB", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("NS", "Story", assigneeId: "d1"), 1m));
        var middle = Sprint(2, sMid, sMidEnd,
            Membership(TicketOf("MB", "Bug", assigneeId: "d1"), 1m),
            Membership(TicketOf("MS", "Story", assigneeId: "d1"), 9m));
        var oldest = Sprint(1, sOldest, sOldestEnd,
            Membership(TicketOf("OB", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("OS", "Story", assigneeId: "d1"), 1m));
        var transitions = new List<StatusTransition>
        {
            Completed("NB", sNewest.AddDays(3)), Completed("NS", sNewest.AddDays(3)),
            Completed("MB", sMid.AddDays(3)), Completed("MS", sMid.AddDays(3)),
            Completed("OB", sOldest.AddDays(3)), Completed("OS", sOldest.AddDays(3))
        };

        var result = Analyzer.ComputeMultiSprint(
            [newest], [newest, middle, oldest], [dev],
            Settings(alertThreshold: 50, consecutiveSprintCount: 3), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        // Correct: count newest (1), hit middle below ⇒ break ⇒ streak 1.
        // Mutant (break→;): would continue and also count oldest ⇒ streak 2.
        alert.ConsecutiveSprintCount.Should().Be(1);
        alert.IsActive.Should().BeFalse(); // 1 >= 3 is false
    }

    // ------------------------------------------------------------------
    // BR-19 / L256: alert walks allClosedSprints OrderByDescending(StartDate).
    //   Mutant: OrderByDescending → OrderBy. With a newest zero-SP sprint and an
    //   older above-threshold streak, descending hits the zero first (streak 0);
    //   ascending would start at the oldest and count up. Distinguishes the order.
    // ------------------------------------------------------------------
    [Fact]
    public void Alert_WalksMostRecentFirst_NotOldestFirst()
    {
        var dev = Dev("d1");
        // Oldest two sprints are above threshold; the NEWEST has zero completed SP.
        var sOld1 = Sprint(1, new(2024, 1, 1), new(2024, 1, 14),
            Membership(TicketOf("OB1", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("OS1", "Story", assigneeId: "d1"), 1m));
        var sOld2 = Sprint(2, new(2024, 1, 15), new(2024, 1, 28),
            Membership(TicketOf("OB2", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("OS2", "Story", assigneeId: "d1"), 1m));
        var sNew = Sprint(3, new(2024, 2, 1), new(2024, 2, 14),
            Membership(TicketOf("NB", "Bug", assigneeId: "d1"), 9m)); // present, NOT completed ⇒ 0 SP
        var transitions = new List<StatusTransition>
        {
            Completed("OB1", new(2024, 1, 7)), Completed("OS1", new(2024, 1, 7)),
            Completed("OB2", new(2024, 1, 20)), Completed("OS2", new(2024, 1, 20))
        };

        var result = Analyzer.ComputeMultiSprint(
            [sOld1], [sOld1, sOld2, sNew], [dev],
            Settings(alertThreshold: 50, consecutiveSprintCount: 1), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        // Descending: newest (zero SP) breaks immediately ⇒ streak 0.
        // Ascending mutant would start at sOld1 (above) ⇒ streak ≥ 1.
        alert.ConsecutiveSprintCount.Should().Be(0);
        alert.IsActive.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // BR-17 / L352/353: FilterMemberships subTeam branch (team path).
    //   if (!string.IsNullOrWhiteSpace(subTeam))
    //       result = result.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam);
    //   Mutants: un-negate the IsNullOrWhiteSpace (filter when blank), the
    //   string-literal swaps, and '==' → '!='. A non-blank subTeam must KEEP the
    //   matching-sub-team membership and DROP the other.
    // ------------------------------------------------------------------
    [Fact]
    public void TeamPath_FiltersMembershipsByAssigneeSubTeam_CaseSensitiveEquality()
    {
        // Two memberships, distinct assignee sub-teams. subTeam="Alpha" keeps only the Alpha one.
        var alpha = Membership(TicketOf("S-A", "Story", assigneeId: "a1", assigneeSubTeam: "Alpha"), 5m);
        var beta = Membership(TicketOf("S-B", "Story", assigneeId: "b1", assigneeSubTeam: "Beta"), 7m);
        var sprint = Sprint(1, S1Start, S1End, alpha, beta);
        var transitions = new List<StatusTransition> { Completed("S-A", S1Mid), Completed("S-B", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: "Alpha");

        // Only the Alpha membership survives the team-path filter (== match, not !=).
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
        result.TeamMetrics.TotalCompletedSp.Should().Be(5.0m);
    }

    // Control: a blank subTeam must NOT filter the team path (un-negated mutant
    // on L352 would erroneously drop everything / keep wrong rows).
    [Fact]
    public void TeamPath_BlankSubTeam_KeepsAllMemberships()
    {
        var alpha = Membership(TicketOf("S-A", "Story", assigneeId: "a1", assigneeSubTeam: "Alpha"), 5m);
        var beta = Membership(TicketOf("S-B", "Story", assigneeId: "b1", assigneeSubTeam: "Beta"), 7m);
        var sprint = Sprint(1, S1Start, S1End, alpha, beta);
        var transitions = new List<StatusTransition> { Completed("S-A", S1Mid), Completed("S-B", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: "");

        // Blank subTeam ⇒ no membership filter ⇒ both count.
        result.TeamMetrics.TotalCompletedSp.Should().Be(12.0m);
    }

    // ------------------------------------------------------------------
    // BR-17 / L354/355: excludedDeveloperIds filter keeps null-assignee, drops matches.
    //   `excludedDeveloperIds is { Count: > 0 }` then
    //   Where(m => m.Ticket?.AssigneeId == null || !excludedDeveloperIds.Contains(...))
    //   L354 mutant: '> 0' → '>= 0' would activate even for an empty set.
    //   Pin: an EMPTY excludedDeveloperIds set is a no-op (matching assignee kept).
    // ------------------------------------------------------------------
    [Fact]
    public void EmptyExcludedDeveloperIds_IsANoOp_KeepsMatchingAssignee()
    {
        var story = Membership(TicketOf("S1", "Story", assigneeId: "d1"), 5m);
        var sprint = Sprint(1, S1Start, S1End, story);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null,
            excludedDeveloperIds: []);

        // Empty exclusion set ⇒ assignee d1's story is retained.
        result.TeamMetrics.TotalNonBugSp.Should().Be(5.0m);
    }

    // ------------------------------------------------------------------
    // BR-18 / L365/366: GetDeveloperMemberships (per-developer + alert path).
    //   Where(m => m.Ticket?.AssigneeId == developerId &&
    //              (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
    //   Mutants: '&&'→'||' (L365), '||'→'&&' (L366), string-literal swaps,
    //   '=='→'!=' on the sub-team check. Arrange one developer whose own ticket
    //   is in the WRONG sub-team ⇒ with a non-blank subTeam the developer's rows
    //   are empty; flipping '||'→'&&' or '=='→'!=' would wrongly include/exclude.
    // ------------------------------------------------------------------
    [Fact]
    public void DeveloperMemberships_RequireAssigneeAndSubTeam_BothMustHold()
    {
        var dev = Dev("d1", subTeam: "Alpha");
        // dev's only ticket is assigned to d1 but its Assignee.SubTeam is "Beta".
        var wrongSubTeam = Membership(
            TicketOf("S1", "Story", assigneeId: "d1", assigneeSubTeam: "Beta"), 5m);
        var sprint = Sprint(1, S1Start, S1End, wrongSubTeam);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid) };

        // subTeam="Alpha": developer d1 passes the developer-level filter (FilterDevelopers
        // matches SubTeam "Alpha"), but the membership's Assignee.SubTeam is "Beta" ≠ "Alpha",
        // so GetDeveloperMemberships must return NOTHING for this developer.
        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(), transitions, subTeam: "Alpha");

        var entry = result.Developers.Single();
        // AssigneeId matches but sub-team does not ⇒ '||'→'&&' wrong, '=='→'!=' wrong.
        entry.CompletedSp.Should().Be(0m);
        entry.BugTicketCount.Should().Be(0);
        entry.NonBugTicketCount.Should().Be(0);
    }

    // Companion: when BOTH conditions hold (assignee matches AND sub-team matches),
    // the membership IS counted — the positive half that distinguishes the filter
    // from "always empty".
    [Fact]
    public void DeveloperMemberships_AreCounted_When_AssigneeAndSubTeamBothMatch()
    {
        var dev = Dev("d1", subTeam: "Alpha");
        var rightSubTeam = Membership(
            TicketOf("S1", "Story", assigneeId: "d1", assigneeSubTeam: "Alpha"), 5m);
        var sprint = Sprint(1, S1Start, S1End, rightSubTeam);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(), transitions, subTeam: "Alpha");

        var entry = result.Developers.Single();
        entry.CompletedSp.Should().Be(5.0m);
        entry.NonBugTicketCount.Should().Be(1);
    }

    // BR-18: GetDeveloperMemberships AssigneeId match is case-SENSITIVE ordinal ==.
    //   A ticket assigned "D1" must NOT match developer id "d1".
    [Fact]
    public void DeveloperMemberships_AssigneeIdMatch_IsCaseSensitive()
    {
        var dev = Dev("d1");
        var wrongCaseAssignee = Membership(TicketOf("S1", "Story", assigneeId: "D1"), 5m);
        var sprint = Sprint(1, S1Start, S1End, wrongCaseAssignee);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(), transitions, subTeam: null);

        // "D1" != "d1" ordinal ⇒ no membership attributed to developer d1.
        result.Developers.Single().CompletedSp.Should().Be(0m);
    }

    // ------------------------------------------------------------------
    // BR-23 / L377: BuildIssueTypeBreakdown groups by `m.Ticket?.IssueType ?? "Unknown"`.
    //   Mutant: replace "Unknown" with "". A null IssueType ticket must group
    //   under the literal "Unknown".
    // BR-23 / L380: spTotal = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m).
    //   Mutants: Sum→Max (need ≥2 in a group with unequal SP), and '?? 0m'→'0m'
    //   (left removed) — a null-effective-SP membership must contribute 0, not crash.
    // ------------------------------------------------------------------
    [Fact]
    public void IssueTypeBreakdown_NullIssueType_GroupsAsUnknown_AndSumsSp()
    {
        // A ticket with a genuinely-null IssueType. Two such memberships with
        // unequal SP (4 and 6) ⇒ group "Unknown", SpTotal = 10 (Sum, not Max=6).
        var n1 = Membership(TicketWithNullIssueType("U1", 4m), 4m);
        var n2 = Membership(TicketWithNullIssueType("U2", 6m), 6m);
        var sprint = Sprint(1, S1Start, S1End, n1, n2);
        var transitions = new List<StatusTransition> { Completed("U1", S1Mid), Completed("U2", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(), transitions, subTeam: null);

        var unknown = result.IssueTypeBreakdown.Single();
        unknown.IssueType.Should().Be("Unknown");      // L377: kills "Unknown"→""
        unknown.TicketCount.Should().Be(2);
        unknown.SpTotal.Should().Be(10.0m);            // L380: Sum (kills Sum→Max=6)
    }

    // BR-23 / L380 '?? 0m' (left removed) — a completed membership whose GetEffectiveSp is
    // null (non-bug, null StoryPoints) must contribute 0 to SpTotal without error,
    // and a sibling with real SP must still sum in.
    [Fact]
    public void IssueTypeBreakdown_NullEffectiveSp_ContributesZero()
    {
        // Two stories in one group: one with SP 5, one with null SP (effective null ⇒ 0).
        var withSp = Membership(TicketOf("S1", "Story"), 5m);
        var nullSp = Membership(TicketOf("S2", "Story"), storyPoints: null);
        var sprint = Sprint(1, S1Start, S1End, withSp, nullSp);
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid), Completed("S2", S1Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(defaultSpPerBug: 0), transitions, subTeam: null);

        var story = result.IssueTypeBreakdown.Single(e => e.IssueType == "Story");
        story.TicketCount.Should().Be(2);
        story.SpTotal.Should().Be(5.0m);   // 5 + 0 (kills '?? 0m' removal)
    }

    // ------------------------------------------------------------------
    // BR-25 / L58-64: the multi-sprint issue-type breakdown is pooled across ALL
    //   target sprints (SelectMany), each completed membership contributing once.
    //   A Story completed in S1 and another Story completed in S2 ⇒ ONE "Story"
    //   group with count 2 (kills a per-sprint-only / first-sprint-only mutant).
    // ------------------------------------------------------------------
    [Fact]
    public void MultiSprint_IssueTypeBreakdown_IsPooledAcrossAllTargetSprints()
    {
        var s1 = Sprint(1, S1Start, S1End, Membership(TicketOf("S1", "Story"), 3m));
        var s2 = Sprint(2, S2Start, S2End, Membership(TicketOf("S2", "Story"), 4m));
        var transitions = new List<StatusTransition> { Completed("S1", S1Mid), Completed("S2", S2Mid) };

        var result = Analyzer.ComputeMultiSprint(
            [s1, s2], [s1, s2], [], Settings(), transitions, subTeam: null);

        var story = result.IssueTypeBreakdown.Single();
        story.IssueType.Should().Be("Story");
        story.TicketCount.Should().Be(2);          // both sprints pooled
        story.SpTotal.Should().Be(7.0m);           // 3 + 4
    }

    // ------------------------------------------------------------------
    // BR-36: per-developer alert is evaluated against the FULL allClosedSprints
    //   history, NOT just the target sprints. The target window is a single sprint
    //   yet the alert streak counts an additional older closed sprint outside it.
    // ------------------------------------------------------------------
    [Fact]
    public void Alert_UsesFullClosedSprintHistory_NotJustTargetWindow()
    {
        var dev = Dev("d1");
        // Target window = S2 only, but BOTH S1 and S2 are above threshold and closed.
        var s1 = Sprint(1, S1Start, S1End,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 1m)); // ratio 90
        var s2 = Sprint(2, S2Start, S2End,
            Membership(TicketOf("B2", "Bug", assigneeId: "d1"), 9m),
            Membership(TicketOf("S2", "Story", assigneeId: "d1"), 1m)); // ratio 90
        var transitions = new List<StatusTransition>
        {
            Completed("B1", S1Mid), Completed("S1", S1Mid),
            Completed("B2", S2Mid), Completed("S2", S2Mid)
        };

        // targetSprints = [s2] only; allClosedSprints = [s1, s2].
        var result = Analyzer.ComputeMultiSprint(
            [s2], [s1, s2], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 2), transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        // Streak counts BOTH closed sprints (2), even though the report window is one sprint.
        alert.ConsecutiveSprintCount.Should().Be(2);
        alert.IsActive.Should().BeTrue();
    }
}
