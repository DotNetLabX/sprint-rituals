using AwesomeAssertions;
using Blocks.Contracts.Grpc.DataContracts;
using Fokus.API.Grpc;
using Fokus.API.Tests._Fixtures;
using Fokus.Domain;
using Fokus.Domain.Analytics;
using Fokus.Persistence;
using Fokus.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fokus.API.Tests.Grpc;

/// <summary>
/// Tests for <see cref="SprintAnalyticsGrpcService.GetSprintSummaryAsync"/> — specifically the
/// enriched mapping of sprint identity and per-rate RAG fields added in RA4 (Step 1).
/// Uses a shared SQLite in-memory connection with real repositories and real analyzer instances
/// (analyzers are pure domain logic — no external dependencies).
/// </summary>
public class SprintAnalyticsGrpcServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FokusDbContext _db;
    private readonly SprintAnalyticsGrpcService _sut;

    public SprintAnalyticsGrpcServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FokusDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new FokusDbContext(options);
        _db.Database.EnsureCreated();

        var sprintRepo = new SprintRepository(_db);
        var appSettingsRepo = new AppSettingsRepository(_db);
        var developerRepo = new DeveloperRepository(_db);
        var ticketRepo = new TicketRepository(_db);
        var testExecutionRepo = new TestExecutionRepository(_db);

        // Pure domain calculators — real instances, no mocking needed
        var sprintSummaryAnalyzer = new SprintSummaryAnalyzer();
        var scopeChangeAnalyzer = new ScopeChangeAnalyzer();
        var carryOverAnalyzer = new CarryOverAnalyzer();
        var qaMetricsAnalyzer = new QaMetricsAnalyzer();

        _sut = new SprintAnalyticsGrpcService(
            sprintRepo,
            appSettingsRepo,
            developerRepo,
            ticketRepo,
            testExecutionRepo,
            sprintSummaryAnalyzer,
            scopeChangeAnalyzer,
            carryOverAnalyzer,
            qaMetricsAnalyzer);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // --- Slice 1: sprint identity fields are populated ---

    [Fact]
    public async Task Should_PopulateSprintIdentity_When_SummaryRequested()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 10, name: "Sprint Alpha");
        var ticket = TestData.FeatureTicket("FOK-10");
        var membership = TestData.CommittedMembership(sprintId: 10, ticketId: "FOK-10",
            storyPoints: 5m, finalStatus: "Done", ticket: ticket,
            sprintStart: sprint.StartDate);

        sprint.AddMembership(membership);
        _db.Tickets.Add(ticket);
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        _db.StatusTransitions.Add(new StatusTransition
        {
            TicketId = "FOK-10",
            FromStatus = "In Progress",
            ToStatus = "Done",
            Timestamp = sprint.StartDate.AddDays(7),
        });
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetSprintSummaryAsync(
            new SprintQuery { SprintId = 10 }, ct);

        result.SprintId.Should().Be(10);
        result.SprintName.Should().Be("Sprint Alpha");
        result.StartDate.Should().Be(sprint.StartDate);
        result.EndDate.Should().Be(sprint.EndDate);
    }

    // --- Slice 2: per-rate RAG fields are populated ---

    [Fact]
    public async Task Should_PopulatePerRateRags_When_SummaryRequested()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 20, name: "Sprint Beta");
        var ticket = TestData.FeatureTicket("FOK-20");
        var membership = TestData.CommittedMembership(sprintId: 20, ticketId: "FOK-20",
            storyPoints: 5m, finalStatus: "Done", ticket: ticket,
            sprintStart: sprint.StartDate);

        sprint.AddMembership(membership);
        _db.Tickets.Add(ticket);
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        _db.StatusTransitions.Add(new StatusTransition
        {
            TicketId = "FOK-20",
            FromStatus = "In Progress",
            ToStatus = "Done",
            Timestamp = sprint.StartDate.AddDays(7),
        });
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetSprintSummaryAsync(
            new SprintQuery { SprintId = 20 }, ct);

        result.CompletionRag.Should().BeOneOf("green", "amber", "red");
        result.DisruptionRag.Should().BeOneOf("green", "amber", "red");
        result.CarryOverRag.Should().BeOneOf("green", "amber", "red");
    }

    // --- Slice 3: RAGs match HealthScoreData bands (green path for 100% completion, 0% disruption) ---

    [Fact]
    public async Task Should_ReflectHealthBand_When_CompletionIs100PercentAndNoDisruption()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 30, name: "Sprint Gamma");
        var ticket = TestData.FeatureTicket("FOK-30");
        var membership = TestData.CommittedMembership(sprintId: 30, ticketId: "FOK-30",
            storyPoints: 5m, finalStatus: "Done", ticket: ticket,
            sprintStart: sprint.StartDate);

        sprint.AddMembership(membership);
        _db.Tickets.Add(ticket);
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        _db.StatusTransitions.Add(new StatusTransition
        {
            TicketId = "FOK-30",
            FromStatus = "In Progress",
            ToStatus = "Done",
            Timestamp = sprint.StartDate.AddDays(7),
        });
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetSprintSummaryAsync(
            new SprintQuery { SprintId = 30 }, ct);

        // 100% completion → green (default threshold: completion green >= 0.8)
        result.CompletionRag.Should().Be("green");
        // 0% disruption → green (lower-is-better, disruption green <= threshold)
        result.DisruptionRag.Should().Be("green");
    }

    // --- Slice 4 (RA5-D Rule 7): TopScopeAdditions ordered most-recently-added first ---

    [Fact]
    public async Task Should_OrderScopeAdditions_MostRecentlyAddedFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 40, name: "Sprint Delta");

        // Three mid-sprint additions at different days. Story points are deliberately INVERTED
        // relative to recency so a story-point ordering would produce a different result —
        // this pins AddedAt-desc ordering, not SP-desc.
        var oldest = TestData.MidSprintAddition(sprintId: 40, ticketId: "FOK-OLD",
            storyPoints: 13m, ticket: TestData.FeatureTicket("FOK-OLD"), sprintStart: sprint.StartDate, addedDaysAfterStart: 3);
        var middle = TestData.MidSprintAddition(sprintId: 40, ticketId: "FOK-MID",
            storyPoints: 8m, ticket: TestData.FeatureTicket("FOK-MID"), sprintStart: sprint.StartDate, addedDaysAfterStart: 6);
        var newest = TestData.MidSprintAddition(sprintId: 40, ticketId: "FOK-NEW",
            storyPoints: 2m, ticket: TestData.FeatureTicket("FOK-NEW"), sprintStart: sprint.StartDate, addedDaysAfterStart: 9);

        sprint.AddMembership(oldest);
        sprint.AddMembership(middle);
        sprint.AddMembership(newest);
        _db.Tickets.AddRange(oldest.Ticket, middle.Ticket, newest.Ticket);
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetSprintSummaryAsync(new SprintQuery { SprintId = 40 }, ct);

        // Most-recently-added first (AddedAt desc), not by story points (which are inverted).
        result.TopScopeAdditions.Select(a => a.TicketKey)
            .Should().Equal("FOK-NEW", "FOK-MID", "FOK-OLD");
    }

    // --- Slice 5 (RA5-D): ScopeAdditionTotal counts the SAME predicate as the list (bugs excluded) ---

    [Fact]
    public async Task Should_CountTrueTotal_OverSamePredicateAsList_ExcludingBugs()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 50, name: "Sprint Epsilon");

        // 6 non-bug mid-sprint additions (more than the top-5 cap) + 2 mid-sprint BUG additions.
        // The true total must count only the 6 non-bug additions — matching the list's predicate —
        // so "+N more" (total − shown) can never promise a bug the list would never show.
        var members = new List<SprintMembership>();
        for (var i = 1; i <= 6; i++)
        {
            // Day i+2 → clearly past the 2-day planning window so all 6 qualify.
            members.Add(TestData.MidSprintAddition(sprintId: 50, ticketId: $"FOK-5{i}",
                storyPoints: i, ticket: TestData.FeatureTicket($"FOK-5{i}"),
                sprintStart: sprint.StartDate, addedDaysAfterStart: i + 2));
        }
        members.Add(TestData.MidSprintBugAddition(sprintId: 50, ticketId: "BUG-51",
            sprintStart: sprint.StartDate, addedDaysAfterStart: 9));
        members.Add(TestData.MidSprintBugAddition(sprintId: 50, ticketId: "BUG-52",
            sprintStart: sprint.StartDate, addedDaysAfterStart: 10));

        foreach (var m in members)
        {
            sprint.AddMembership(m);
            _db.Tickets.Add(m.Ticket);
        }
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetSprintSummaryAsync(new SprintQuery { SprintId = 50 }, ct);

        // True total = 6 non-bug additions (bugs excluded), NOT all 8 additions.
        result.ScopeAdditionTotal.Should().Be(6, "bugs are excluded from the total, same as the list");
        // List is capped at 5 — so overflow downstream is 6 − shown, never promising a bug.
        result.TopScopeAdditions.Should().HaveCount(5, "server caps the displayed list at 5");
        result.TopScopeAdditions.Should().OnlyContain(a => a.TicketKey.StartsWith("FOK-"),
            "no bug ticket may appear in the displayed additions");
    }

    // --- Slice 6 (RA5-D case bug): MidSprintAdditionCount > 0 for a sprint with additions ---

    [Fact]
    public async Task Should_CountMidSprintAdditions_When_AdditionsExist_CaseInsensitive()
    {
        var ct = TestContext.Current.CancellationToken;
        var sprint = TestData.ClosedSprint(id: 60, name: "Sprint Zeta");

        // Two non-committed mid-sprint additions → two "added" rows in the scope-change event table.
        // Before the fix, MapToDisruptionMetrics compared Action == "Added" (capital A) against the
        // lowercase "added" literal and always returned 0.
        var a1 = TestData.MidSprintAddition(sprintId: 60, ticketId: "FOK-61",
            storyPoints: 5m, ticket: TestData.FeatureTicket("FOK-61"), sprintStart: sprint.StartDate, addedDaysAfterStart: 4);
        var a2 = TestData.MidSprintAddition(sprintId: 60, ticketId: "FOK-62",
            storyPoints: 3m, ticket: TestData.FeatureTicket("FOK-62"), sprintStart: sprint.StartDate, addedDaysAfterStart: 6);

        sprint.AddMembership(a1);
        sprint.AddMembership(a2);
        _db.Tickets.AddRange(a1.Ticket, a2.Ticket);
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);

        var result = await _sut.GetDisruptionMetricsAsync(new SprintQuery { SprintId = 60 }, ct);

        result.MidSprintAdditionCount.Should().Be(2,
            "the case-insensitive Action match now counts the lowercase \"added\" event rows");
    }
}
