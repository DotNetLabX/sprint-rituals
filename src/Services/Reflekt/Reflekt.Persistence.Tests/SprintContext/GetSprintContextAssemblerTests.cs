using AwesomeAssertions;
using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Reflekt.API.Features.RetroSessions.GetSprintContext;
using Reflekt.Domain.FokusSprints;
using Reflekt.Persistence;
using Reflekt.Persistence.Repositories;
using Xunit;

namespace Reflekt.Persistence.Tests.SprintContext;

/// <summary>
/// Behavioral tests for <see cref="GetSprintContextAssembler"/> — the assembly logic
/// for the sprint-context panel (Step 4 / RA4).
///
/// Approach: real SQLite in-memory for the snapshot repo; hand-rolled fake for ISprintAnalyticsService.
/// </summary>
public class GetSprintContextAssemblerTests : IDisposable
{
    private readonly ReflektDbContext _db;
    private readonly FokusSprintSnapshotRepository _snapshotRepo;

    public GetSprintContextAssemblerTests()
    {
        var options = new DbContextOptionsBuilder<ReflektDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new ReflektDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _snapshotRepo = new FokusSprintSnapshotRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    private GetSprintContextAssembler MakeAssembler(ISprintAnalyticsService grpc)
        => new(_snapshotRepo, grpc, NullLogger<GetSprintContextAssembler>.Instance);

    private static AnonymizedSprintSummary LiveSummary(
        int sprintId,
        string name,
        DateTime start,
        DateTime end,
        decimal completionRate = 1m,
        decimal disruptionRate = 0m,
        decimal carryOverRate = 0m,
        string completionRag = "green",
        string disruptionRag = "green",
        string carryOverRag = "green",
        List<ScopeAdditionEntry>? topScopeAdditions = null,
        int? scopeAdditionTotal = null)
        => new()
        {
            SprintId = sprintId,
            SprintName = name,
            StartDate = start,
            EndDate = end,
            CompletionRate = completionRate,
            DisruptionRate = disruptionRate,
            CarryOverRate = carryOverRate,
            CompletionRag = completionRag,
            DisruptionRag = disruptionRag,
            CarryOverRag = carryOverRag,
            HealthScore = 100m,
            HealthRag = "green",
            TopScopeAdditions = topScopeAdditions ?? [],
            // Defaults to the displayed-list size when not specified (no overflow) — the server
            // always sends the true total, but most fixtures only care about the list itself.
            ScopeAdditionTotal = scopeAdditionTotal ?? (topScopeAdditions?.Count ?? 0),
        };

    // ─── Slice 1: single ref → live section ───────────────────────────────────

    [Fact]
    public async Task Should_ReturnLiveSection_When_GrpcSucceeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        grpc.Seed(LiveSummary(sprintId: 1, name: "Sprint Alpha", start: start, end: start.AddDays(14)));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([1], ct);

        sections.Should().HaveCount(1);
        var s = sections[0];
        s.SprintId.Should().Be(1);
        s.SprintName.Should().Be("Sprint Alpha");
        s.Freshness.Should().Be("live");
        s.CompletionRate.Should().NotBeNull();
        s.StartDate.Should().Be(start);
    }

    // ─── Slice 2: two refs → ordered by StartDate ascending ──────────────────

    [Fact]
    public async Task Should_OrderSections_ByStartDateAscending()
    {
        var ct = TestContext.Current.CancellationToken;
        var early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var late = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var grpc = new StubSprintAnalytics();
        // Seed in reverse order — assembler must sort them
        grpc.Seed(LiveSummary(sprintId: 2, name: "Sprint 2", start: late, end: late.AddDays(14)));
        grpc.Seed(LiveSummary(sprintId: 1, name: "Sprint 1", start: early, end: early.AddDays(14)));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([2, 1], ct);

        sections.Should().HaveCount(2);
        sections[0].SprintId.Should().Be(1, "earliest start comes first");
        sections[1].SprintId.Should().Be(2);
    }

    // ─── Slice 3: gRPC throws + snapshot exists → "cached" ───────────────────

    [Fact]
    public async Task Should_ReturnCachedSection_When_GrpcFailsAndSnapshotExists()
    {
        var ct = TestContext.Current.CancellationToken;

        // Seed a snapshot for sprint 10
        var snapshot = FokusSprintSnapshot.CreateOrUpdate(10, "Sprint 10", boardId: 1,
            syncedAt: DateTime.UtcNow, state: "ACTIVE");
        await _snapshotRepo.UpsertAsync(snapshot, ct);
        await _snapshotRepo.SaveChangesAsync(ct);

        var grpc = new ThrowingSprintAnalytics();
        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([10], ct);

        sections.Should().HaveCount(1);
        var s = sections[0];
        s.SprintId.Should().Be(10);
        s.SprintName.Should().Be("Sprint 10");
        s.Freshness.Should().Be("cached");
        s.CompletionRate.Should().BeNull("metric numbers must not appear in cached sections (PC3)");
        s.AddedMidSprint.Should().BeNull();
    }

    // ─── Slice 4: gRPC throws + no snapshot → "unavailable" ──────────────────

    [Fact]
    public async Task Should_ReturnUnavailableSection_When_GrpcFailsAndNoSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        var grpc = new ThrowingSprintAnalytics();
        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([99], ct);

        sections.Should().HaveCount(1);
        var s = sections[0];
        s.SprintId.Should().Be(99);
        s.SprintName.Should().BeNull();
        s.Freshness.Should().Be("unavailable");
    }

    // ─── Slice 5: Rule 6 gate ON — disruption amber → scope additions shown ──

    [Fact]
    public async Task Should_IncludeScopeAdditions_When_DisruptionRagIsAmber()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        grpc.Seed(LiveSummary(
            sprintId: 5, name: "Sprint 5", start: start, end: start.AddDays(14),
            disruptionRag: "amber",
            topScopeAdditions:
            [
                new ScopeAdditionEntry { TicketKey = "F-1", Summary = "Feature A", StoryPoints = 5m },
                new ScopeAdditionEntry { TicketKey = "F-2", Summary = "Feature B", StoryPoints = 3m },
            ]));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([5], ct);

        var s = sections[0];
        s.AddedMidSprint.Should().NotBeNull();
        s.AddedMidSprint!.Should().HaveCount(2);
        s.AddedMidSprintOverflow.Should().Be(0);
    }

    // ─── Slice 6: Rule 6 gate OFF — disruption green → scope additions hidden ─

    [Fact]
    public async Task Should_HideScopeAdditions_When_DisruptionRagIsGreen()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        grpc.Seed(LiveSummary(
            sprintId: 6, name: "Sprint 6", start: start, end: start.AddDays(14),
            disruptionRag: "green",
            topScopeAdditions:
            [
                new ScopeAdditionEntry { TicketKey = "F-3", Summary = "Feature C", StoryPoints = 5m },
            ]));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([6], ct);

        var s = sections[0];
        s.AddedMidSprint.Should().BeNull("disruption is green — scope additions are suppressed");
    }

    // ─── Slice 7: overflow = TRUE total − shown (RA5-D), not list.Count − 3 ──

    [Fact]
    public async Task Should_CapScopeAdditions_At3_And_SetTrueOverflow()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        // The contract carries the top-5 list AND a true total of 9 mid-sprint additions.
        // Overflow must be the TRUE remainder (9 − 3 displayed = 6), NOT list.Count − 3 (= 2).
        grpc.Seed(LiveSummary(
            sprintId: 7, name: "Sprint 7", start: start, end: start.AddDays(14),
            disruptionRag: "red",
            topScopeAdditions:
            [
                new ScopeAdditionEntry { TicketKey = "F-10", Summary = "A", StoryPoints = 8m },
                new ScopeAdditionEntry { TicketKey = "F-11", Summary = "B", StoryPoints = 7m },
                new ScopeAdditionEntry { TicketKey = "F-12", Summary = "C", StoryPoints = 6m },
                new ScopeAdditionEntry { TicketKey = "F-13", Summary = "D", StoryPoints = 5m },
                new ScopeAdditionEntry { TicketKey = "F-14", Summary = "E", StoryPoints = 4m },
            ],
            scopeAdditionTotal: 9));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([7], ct);

        var s = sections[0];
        s.AddedMidSprint!.Should().HaveCount(3, "display limit is 3");
        s.AddedMidSprintOverflow.Should().Be(6, "true total 9 − 3 displayed = 6 overflow");
    }

    // ─── Slice 7b: list order is preserved from the contract (most-recently-added first) ──

    [Fact]
    public async Task Should_PreserveContractOrder_In_DisplayedScopeAdditions()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        // Fokus already orders most-recently-added-first; the assembler must pass that order
        // through unchanged (no re-sort). Story points here are NOT descending, so a re-sort
        // would reorder them.
        grpc.Seed(LiveSummary(
            sprintId: 8, name: "Sprint 8", start: start, end: start.AddDays(14),
            disruptionRag: "red",
            topScopeAdditions:
            [
                new ScopeAdditionEntry { TicketKey = "NEW", Summary = "newest", StoryPoints = 1m },
                new ScopeAdditionEntry { TicketKey = "MID", Summary = "middle", StoryPoints = 8m },
                new ScopeAdditionEntry { TicketKey = "OLD", Summary = "oldest", StoryPoints = 3m },
            ],
            scopeAdditionTotal: 3));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([8], ct);

        var s = sections[0];
        // Contract order is preserved, not re-sorted (story points are not descending here).
        s.AddedMidSprint!.Select(a => a.TicketKey)
            .Should().Equal("NEW", "MID", "OLD");
        s.AddedMidSprintOverflow.Should().Be(0, "true total 3 − 3 displayed = 0 overflow");
    }

    // ─── Slice 8: structural assertion — no assignee data in any live section ─

    [Fact]
    public async Task Should_ContainNoAssigneeData_In_LiveSection()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var grpc = new StubSprintAnalytics();
        grpc.Seed(LiveSummary(
            sprintId: 8, name: "Sprint 8", start: start, end: start.AddDays(14),
            disruptionRag: "amber",
            topScopeAdditions:
            [
                new ScopeAdditionEntry { TicketKey = "F-20", Summary = "Some feature", StoryPoints = 5m },
            ]));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([8], ct);

        var s = sections[0];
        // ScopeAdditionDto has exactly 3 fields: TicketKey, Summary, StoryPoints.
        // No assignee, no developer name, no account ID (ADR-019 Rule 4).
        var addition = s.AddedMidSprint![0];
        addition.TicketKey.Should().Be("F-20");
        addition.Summary.Should().Be("Some feature");
        addition.StoryPoints.Should().Be(5m);

        // Verify the DTO has no properties beyond the three expected fields
        var dtoProperties = typeof(ScopeAdditionDto).GetProperties().Select(p => p.Name);
        dtoProperties.Should().BeEquivalentTo(["TicketKey", "Summary", "StoryPoints"],
            "ScopeAdditionDto must expose exactly these three fields — no assignee data (Rule 4)");
    }

    // ─── Slice 9: one sprint fails gRPC, others still return live ─────────────

    [Fact]
    public async Task Should_IsolateFailing_Sprint_WhileOthersReturnLive()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var grpc = new SelectiveThrowingSprintAnalytics(throwForSprintId: 50);
        grpc.Seed(LiveSummary(sprintId: 49, name: "Sprint 49", start: start, end: start.AddDays(14)));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([49, 50], ct);

        sections.Should().HaveCount(2);
        sections.Single(s => s.SprintId == 49).Freshness.Should().Be("live");
        sections.Single(s => s.SprintId == 50).Freshness.Should().Be("unavailable",
            "sprint 50 has no snapshot — degrades to unavailable");
    }

    // ─── Slice 10: nulls-last ordering — cached section follows live sections ─

    [Fact]
    public async Task Should_PlaceCachedSections_AfterLiveSections_InOrdering()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Sprint 100: gRPC fails, has snapshot (will be "cached", no StartDate)
        var snapshot = FokusSprintSnapshot.CreateOrUpdate(100, "Sprint 100", boardId: 1,
            syncedAt: DateTime.UtcNow, state: "CLOSED");
        await _snapshotRepo.UpsertAsync(snapshot, ct);
        await _snapshotRepo.SaveChangesAsync(ct);

        var grpc = new SelectiveThrowingSprintAnalytics(throwForSprintId: 100);
        grpc.Seed(LiveSummary(sprintId: 101, name: "Sprint 101", start: start, end: start.AddDays(14)));

        var assembler = MakeAssembler(grpc);
        var sections = await assembler.BuildSectionsAsync([100, 101], ct);

        sections.Should().HaveCount(2);
        // Live sprint (has StartDate) sorts before cached (no StartDate)
        sections[0].SprintId.Should().Be(101, "live section with StartDate sorts first");
        sections[1].SprintId.Should().Be(100, "cached section (no StartDate) sorts last");
    }
}

// ─── Fakes ────────────────────────────────────────────────────────────────────

/// <summary>Configurable stub — returns pre-seeded summaries by SprintId.</summary>
internal sealed class StubSprintAnalytics : ISprintAnalyticsService
{
    private readonly Dictionary<int, AnonymizedSprintSummary> _map = [];

    public void Seed(AnonymizedSprintSummary summary) => _map[summary.SprintId] = summary;

    public Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintQuery request, CancellationToken ct = default)
    {
        if (_map.TryGetValue(request.SprintId, out var summary))
            return Task.FromResult(summary);
        throw new InvalidOperationException($"No seeded summary for sprint {request.SprintId}.");
    }

    public Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");

    public Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");
}

/// <summary>Always throws to simulate Fokus gRPC being unreachable.</summary>
internal sealed class ThrowingSprintAnalytics : ISprintAnalyticsService
{
    public Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintQuery request, CancellationToken ct = default)
        => throw new RpcException("Simulated Fokus unavailability");

    public Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");

    public Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");
}

/// <summary>
/// Throws for a specific SprintId; delegates to a seeded stub for others.
/// Used to verify per-sprint gRPC isolation (one failure must not blank the whole panel).
/// </summary>
internal sealed class SelectiveThrowingSprintAnalytics(int throwForSprintId) : ISprintAnalyticsService
{
    private readonly StubSprintAnalytics _stub = new();

    public void Seed(AnonymizedSprintSummary summary) => _stub.Seed(summary);

    public Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintQuery request, CancellationToken ct = default)
    {
        if (request.SprintId == throwForSprintId)
            throw new RpcException($"Simulated failure for sprint {throwForSprintId}");
        return _stub.GetSprintSummaryAsync(request, ct);
    }

    public Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");

    public Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamQuery request, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed in these tests.");
}

/// <summary>Minimal stand-in for Grpc.Core.RpcException — avoids pulling in the full gRPC package.</summary>
internal sealed class RpcException(string message) : Exception(message);
