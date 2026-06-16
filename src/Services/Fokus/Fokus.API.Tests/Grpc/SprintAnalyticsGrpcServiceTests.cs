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
}
