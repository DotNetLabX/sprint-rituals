using AwesomeAssertions;
using Fokus.Domain;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Cover-from-spec probes for <see cref="BugRatioAnalyzer"/> — the Generate output of the F13 M1
/// merge (registry: docs/kb/golden/BugRatioAnalyzer.md).
///
/// Each test asserts what the F13-BugRatio SPEC commits to
/// (docs/specs/fokus/F13-BugRatio/definition/spec-rules.md); the code-arm suite
/// (BugRatioAnalyzerTests, mutation-gated 100% reachable kill) attests current behavior. Production
/// code is never edited to make these pass.
///
/// History: the original pilot carried two parked reds (SR-28 alert boundary '>' vs '>=';
/// SR-23 no-SP bug vs DefaultSpPerBug substitution). Both divergences were adjudicated on
/// 2026-07-04 — owner verdict: KEEP CODE, spec repaired — so the obsolete red probes were removed;
/// the durable divergence record lives in the registry's Divergence detail + Changelog, and the
/// repaired behavior is pinned by the code-arm suite (Alert_Fires_When_RatioExactlyAtThreshold_...,
/// the BR-15 DefaultSpPerBug tests).
///
/// Remaining probes cover generation-eligible spec-only rules (code-arm unattested): SR-23
/// ticket-metrics half, SR-32 zero-ticket developer visibility, SR-42 priority neutrality.
/// All green = implemented; fact tags per mine-verify-cover-dotnet.
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
    // SR-23 (ticket-metrics half) — spec: no-SP tickets DO count in ticket-based
    // metrics (bug ticket count, issue type breakdown counts). Both arms agree.
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "golden")]
    [Trait("runtime-cost", "fast")]
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
    // SR-32 — spec: active developers with zero completed tickets in the selected
    // sprints appear in the table with all-zero values; they are never hidden.
    // Generate probe (spec-only rule, no code-arm BR attests it).
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "core")]
    [Trait("runtime-cost", "fast")]
    public void SpecSR32_ActiveDeveloperWithZeroCompletedTickets_AppearsWithAllZeroValues()
    {
        var dev = Dev("d1");
        // the sprint's only completed ticket belongs to someone else entirely
        var sprint = Sprint(1, Membership(TicketOf("S1", "Story", assigneeId: "d2"), 5m));
        var transitions = new List<StatusTransition> { Completed("S1") };

        var result = Analyzer.ComputeMultiSprint(
            [sprint], [sprint], [dev], Settings(), transitions, subTeam: null);

        var d1 = result.Developers.Single(d => d.AccountId == "d1");
        d1.CompletedSp.Should().Be(0m, "zero-ticket developers are shown zeroed, never hidden");
        d1.BugRatioPercent.Should().Be(0m);
        d1.BugTicketCount.Should().Be(0);
        d1.NonBugTicketCount.Should().Be(0);
        d1.Alert.IsActive.Should().BeFalse();
    }

    // =====================================================================
    // SR-42 — spec: the ratio treats all bugs equally regardless of priority.
    // Both arms agree (the analyzer never reads Priority).
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "edge")]
    [Trait("runtime-cost", "fast")]
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
