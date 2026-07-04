using AwesomeAssertions;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Cover-from-spec probes for <see cref="CycleTimeAnalyzer"/> — the Generate output of the F12 M1
/// merge (registry: docs/kb/golden/CycleTimeAnalyzer.md).
///
/// Each test asserts what the F12-CycleTime SPEC commits to
/// (docs/specs/fokus/F12-CycleTime/definition/spec-rules.md); the code-arm suite
/// (CycleTimeAnalyzerTests + CycleTimeAnalyzerPropertyTests, mutation-gated 100% reachable kill)
/// attests current behavior. Production code is never edited to make these pass.
///
/// Probes cover the generation-eligible spec-only rules (code-arm unattested):
/// SR-65 calendar-day measurement, SR-68 no-story-points inclusion, SR-73 excluded-from-scope
/// independence. SR-74 (query-time stage config) and SR-78 (one shared pipeline for all issue
/// types) are negative/architectural constraints resolved by inspection — see the registry
/// changelog. All green = implemented; fact tags per mine-verify-cover-dotnet.
/// </summary>
public class CycleTimeSpecArmTests
{
    private static AppSettings Settings(List<string>? excludedFromScope = null) => new()
    {
        WorkflowStages = ["Todo", "In Progress", "Review"],
        DoneStatuses = ["Done", "Closed"],
        CycleTime = new CycleTimeConfig { StartStage = null, EndStage = null },
        ExcludedFromScopeStatuses = excludedFromScope ?? [],
    };

    private static readonly DateTime SprintStart = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SprintEnd = new(2025, 1, 14, 23, 59, 59, DateTimeKind.Utc);

    private static Sprint MakeSprint()
    {
        return new Sprint
        {
            Id = 1,
            Name = "Sprint 1",
            StartDate = SprintStart,
            EndDate = SprintEnd,
            BoardId = 1,
            BoardName = "Test Board",
            State = SprintState.Closed,
            SyncedAt = SprintEnd,
        };
    }

    // One completed ticket, no story points: In Progress on <paramref name="startOfWork"/>,
    // Done + Closed on <paramref name="doneAt"/> (Closed immediately bounds the Done stage to 0).
    private static (Sprint sprint, List<StatusTransition> transitions) MakeScenario(
        DateTime startOfWork, DateTime doneAt)
    {
        var sprint = MakeSprint();
        var ticket = new Ticket
        {
            Id = "T-1",
            Summary = "Probe ticket",
            IssueType = "Story",
            Priority = "Medium",
            CurrentStatus = "Done",
            AssigneeId = null,
            StoryPoints = null,
        };
        sprint.AddMembership(new SprintMembership
        {
            SprintId = sprint.Id,
            TicketId = ticket.Id,
            AddedAt = sprint.StartDate,
            RemovedAt = null,
            FinalStatus = "Done",
            Ticket = ticket,
            Sprint = sprint,
        });

        var transitions = new List<StatusTransition>
        {
            new() { TicketId = "T-1", FromStatus = "Todo", ToStatus = "In Progress", Timestamp = startOfWork, Ticket = null! },
            new() { TicketId = "T-1", FromStatus = "In Progress", ToStatus = "Done", Timestamp = doneAt, Ticket = null! },
            new() { TicketId = "T-1", FromStatus = "Done", ToStatus = "Closed", Timestamp = doneAt, Ticket = null! },
        };
        return (sprint, transitions);
    }

    private static readonly CycleTimeAnalyzer Analyzer = new();

    // =====================================================================
    // SR-65 — spec: duration is measured in calendar days, including weekends
    // and holidays, not business days. Generate probe (no code-arm BR attests
    // the calendar-vs-business distinction explicitly).
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "core")]
    [Trait("runtime-cost", "fast")]
    public void SpecSR65_CycleTime_CountsCalendarDays_IncludingWeekends()
    {
        // Fri 2025-01-03 00:00 → Mon 2025-01-06 00:00 spans Sat 4th + Sun 5th.
        // Business-day counting would yield 1 day; calendar days yield exactly 3.
        var (sprint, transitions) = MakeScenario(
            startOfWork: new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            doneAt: new DateTime(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc));

        var result = Analyzer.ComputeSingleSprint(sprint, null, transitions, Settings(), subTeam: null);

        result.ScatterPlot.Single().CycleTimeDays.Should().Be(3.00m,
            "weekend days count — cycle time is calendar days, not business days");
    }

    // =====================================================================
    // SR-68 — spec: tickets without story points are included in all cycle
    // time calculations (duration-based, not SP-based). Generate probe.
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "core")]
    [Trait("runtime-cost", "fast")]
    public void SpecSR68_TicketWithoutStoryPoints_IsIncludedInAllCalculations()
    {
        var (sprint, transitions) = MakeScenario(
            startOfWork: new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            doneAt: new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc));

        var result = Analyzer.ComputeSingleSprint(sprint, null, transitions, Settings(), subTeam: null);

        result.ScatterPlot.Single().TicketKey.Should().Be("T-1",
            "a no-SP ticket has measurable duration and is never excluded");
        result.IssueTypeBreakdown.Single(e => e.IssueType == "Story").TicketCount.Should().Be(1);
        result.MetricCards.Should().NotBeNull();
    }

    // =====================================================================
    // SR-73 — spec: the excluded-from-scope statuses setting (F10) does not
    // affect cycle time; inclusion is governed solely by done status and
    // start-stage entry. Generate probe: identical input, exclusion setting
    // toggled — results must be identical.
    // =====================================================================

    [Fact]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "edge")]
    [Trait("runtime-cost", "fast")]
    public void SpecSR73_ExcludedFromScopeStatuses_HaveNoEffectOnCycleTime()
    {
        var (sprintA, transitionsA) = MakeScenario(
            startOfWork: new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            doneAt: new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        var (sprintB, transitionsB) = MakeScenario(
            startOfWork: new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            doneAt: new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc));

        var unfiltered = Analyzer.ComputeSingleSprint(
            sprintA, null, transitionsA, Settings(), subTeam: null);
        var withExclusions = Analyzer.ComputeSingleSprint(
            sprintB, null, transitionsB, Settings(excludedFromScope: ["In Progress", "Review"]), subTeam: null);

        withExclusions.ScatterPlot.Should().HaveCount(unfiltered.ScatterPlot.Count,
            "scope exclusion is an F10 concern — cycle time inclusion ignores it");
        withExclusions.ScatterPlot.Single().CycleTimeDays
            .Should().Be(unfiltered.ScatterPlot.Single().CycleTimeDays);
    }
}
