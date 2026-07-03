using AwesomeAssertions;
using Fokus.Domain;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Spec-arm mini-pilot for <see cref="BugRatioAnalyzer"/> — Cover-from-spec probe (AC-6 evidence).
///
/// Each test asserts what the F13-BugRatio SPEC commits to
/// (docs/specs/fokus/F13-BugRatio/definition/spec-rules.md), NOT what the code does. The code-arm
/// suite (BugRatioAnalyzerTests, mutation-gated 100% reachable kill) attests current behavior; where
/// spec and code disagree, the test here goes RED and the red is the finding — a spec-code divergence
/// demanding a triage verdict (code gap vs intentional drift vs stale spec). Production code is never
/// edited to make these pass, and a red test is kept (parked with Skip + the observed result) rather
/// than deleted, so the divergence stays on the record until triaged.
///
/// Rules probed: SR-28 (alert streak boundary), SR-23 (no-SP tickets in SP vs ticket metrics),
/// SR-42 (priority ignored). SR-31 (active developers) was resolved by inspection — implemented
/// upstream in DeveloperRepository (.Where(d => d.IsActive)), out of the analyzer's surface.
/// SR-30's threshold=0 edge is deliberately NOT tested: the spec is ambiguous there (reasoning covers
/// only positive thresholds), and an ambiguous rule must not generate a test.
/// </summary>
public class BugRatioSpecArmPilotTests
{
    private const string DoneStage = "Done";

    private static AppSettings Settings(
        int alertThreshold = 50,
        int consecutiveSprintCount = 2,
        int defaultSpPerBug = 0)
    {
        var s = AppSettings.CreateDefault();
        s.WorkflowStages = ["To Do", "In Progress"];
        s.DoneStatuses = ["Done"];
        s.ExcludedFromScopeStatuses = [];
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

    private static Developer Dev(string id) =>
        new() { Id = id, DisplayName = id, SubTeam = null };

    private static Ticket TicketOf(
        string id, string issueType = "Story", string? assigneeId = null,
        string priority = "Medium", decimal? storyPoints = null)
    {
        var t = new Ticket
        {
            Id = id,
            Summary = id,
            IssueType = issueType,
            Priority = priority,
            CurrentStatus = "Done",
            AssigneeId = assigneeId,
            StoryPoints = storyPoints
        };
        if (assigneeId is not null)
            t.Assignee = new Developer { Id = assigneeId, DisplayName = assigneeId, SubTeam = null };
        return t;
    }

    private static SprintMembership Membership(Ticket ticket, decimal? storyPoints) =>
        new()
        {
            TicketId = ticket.Id,
            FinalStatus = "Done",
            StoryPoints = storyPoints,
            Ticket = ticket,
            AddedAt = new DateTime(2024, 1, 1)
        };

    private static Sprint Sprint(int id, params SprintMembership[] memberships)
    {
        var s = new Sprint { Id = id, Name = $"S{id}", BoardName = "B", StartDate = S1Start, EndDate = S1End };
        foreach (var m in memberships)
            s.AddMembership(m);
        return s;
    }

    private static StatusTransition Completed(string ticketId) =>
        new() { TicketId = ticketId, FromStatus = "In Progress", ToStatus = DoneStage, Timestamp = S1Mid };

    private static readonly DateTime S1Start = new(2024, 1, 1);
    private static readonly DateTime S1End = new(2024, 1, 14);
    private static readonly DateTime S1Mid = new(2024, 1, 7);

    private static readonly BugRatioAnalyzer Analyzer = new();

    // =====================================================================
    // SR-28 — spec: a sprint counts toward the alert streak only when the ratio
    // EXCEEDS the threshold (strict >). A sprint exactly AT the threshold is not
    // above it, so the streak must not count it and the alert must not fire.
    // Code-arm attestation (BR-21): `ratio >= threshold` — inclusive, opposite verdict.
    // =====================================================================

    // OBSERVED RED 2026-07-03: ConsecutiveSprintCount was 1, IsActive true — code counts the
    // at-threshold sprint (`ratio >= threshold`, BugRatioAnalyzer.cs:272), and the code-arm suite
    // deliberately pins that inclusive boundary (Alert_Fires_When_RatioExactlyAtThreshold_...).
    // Divergence, not proven bug: spec says "exceeds", code attests ">=". Pending triage verdict.
    [Fact(Skip = "SPEC-CODE DIVERGENCE (SR-28 vs BR-21): spec 'exceeds' (>) vs code '>=' — pending triage")]
    public void SpecSR28_SprintExactlyAtThreshold_DoesNotCountTowardStreak()
    {
        var dev = Dev("d1");
        // bug 5 SP + story 5 SP completed ⇒ ratio = 50, threshold = 50 ⇒ spec: not "exceeds"
        var sprint = Sprint(1,
            Membership(TicketOf("B1", "Bug", assigneeId: "d1"), 5m),
            Membership(TicketOf("S1", "Story", assigneeId: "d1"), 5m));
        var transitions = new List<StatusTransition> { Completed("B1"), Completed("S1") };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(alertThreshold: 50, consecutiveSprintCount: 1),
            transitions, subTeam: null);

        var alert = result.Developers.Single().Alert;
        alert.ConsecutiveSprintCount.Should().Be(0, "the spec counts only sprints whose ratio EXCEEDS the threshold");
        alert.IsActive.Should().BeFalse();
    }

    // =====================================================================
    // SR-23 — spec: tickets with no story points are excluded from SP metrics
    // (bug SP, non-bug SP, bug ratio %). No default is ever substituted.
    // Code-arm attestation (BR-15): GetEffectiveSp(defaultSpPerBug) gives a
    // no-SP BUG ticket the settings-sourced default — a later-feature amendment.
    // =====================================================================

    // OBSERVED RED 2026-07-03: TotalBugSp was 5 — GetEffectiveSp substitutes DefaultSpPerBug for
    // no-SP bugs (SprintMembership.cs:16). The default came from a later feature; the F13 spec is
    // stale on this point. Divergence is almost certainly intentional drift — pending triage verdict.
    [Fact(Skip = "SPEC-CODE DIVERGENCE (SR-23 vs BR-15): spec excludes no-SP tickets, code substitutes DefaultSpPerBug — pending triage")]
    public void SpecSR23_NoSpBugTicket_ContributesNothingToBugSp()
    {
        // one completed bug with NO story points; DefaultSpPerBug configured to 5
        var sprint = Sprint(1, Membership(TicketOf("B1", "Bug"), storyPoints: null));
        var transitions = new List<StatusTransition> { Completed("B1") };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [], Settings(defaultSpPerBug: 5), transitions, subTeam: null);

        result.TeamMetrics.TotalBugSp.Should().Be(0m, "the spec excludes no-SP tickets from SP metrics entirely");
        result.TeamMetrics.TotalCompletedSp.Should().Be(0m);
    }

    // =====================================================================
    // SR-23 (ticket-metrics half) — spec: no-SP tickets DO count in ticket-based
    // metrics (bug ticket count, issue type breakdown counts). Both arms agree here.
    // =====================================================================

    [Fact]
    public void SpecSR23_NoSpBugTicket_StillCountsInTicketMetrics()
    {
        var dev = Dev("d1");
        var sprint = Sprint(1, Membership(TicketOf("B1", "Bug", assigneeId: "d1"), storyPoints: null));
        var transitions = new List<StatusTransition> { Completed("B1") };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(defaultSpPerBug: 0), transitions, subTeam: null);

        result.Developers.Single().BugTicketCount.Should().Be(1, "no-SP tickets count in ticket-based metrics");
        result.IssueTypeBreakdown.Single(e => e.IssueType == "Bug").TicketCount.Should().Be(1);
    }

    // =====================================================================
    // SR-42 — spec: the ratio treats all bugs equally regardless of priority.
    // Both arms agree (the analyzer never reads Priority).
    // =====================================================================

    [Fact]
    public void SpecSR42_BugPriority_HasNoEffectOnRatio()
    {
        var p1 = Sprint(1,
            Membership(TicketOf("B1", "Bug", priority: "P1"), 4m),
            Membership(TicketOf("S1", "Story"), 6m));
        var p3 = Sprint(1,
            Membership(TicketOf("B1", "Bug", priority: "P3"), 4m),
            Membership(TicketOf("S1", "Story"), 6m));
        var transitions = new List<StatusTransition> { Completed("B1"), Completed("S1") };

        var high = Analyzer.ComputeMultiSprint([p1], [p1], [], Settings(), transitions, subTeam: null);
        var low = Analyzer.ComputeMultiSprint([p3], [p3], [], Settings(), transitions, subTeam: null);

        high.TeamMetrics.BugRatioPercent.Should().Be(low.TeamMetrics.BugRatioPercent);
        high.TeamMetrics.BugRatioPercent.Should().Be(40.0m);
    }
}
