using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.FokusSprints;
using Reflekt.Persistence;
using Reflekt.Persistence.Repositories;
using Xunit;

namespace Reflekt.Persistence.Tests.FokusSprints;

public class FokusSprintSnapshotRepositoryTests : IDisposable
{
    private readonly ReflektDbContext _db;
    private readonly FokusSprintSnapshotRepository _repo;

    public FokusSprintSnapshotRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ReflektDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new ReflektDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _repo = new FokusSprintSnapshotRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    private static FokusSprintSnapshot MakeSnapshot(int sprintId, string state = "ACTIVE")
        => FokusSprintSnapshot.CreateOrUpdate(sprintId, $"Sprint {sprintId}", boardId: 1, syncedAt: DateTime.UtcNow, state: state);

    // --- Slice 1: insert new snapshot ---

    [Fact]
    public async Task Should_InsertSnapshot_When_NoneExists()
    {
        var snapshot = MakeSnapshot(42);

        await _repo.UpsertAsync(snapshot, TestContext.Current.CancellationToken);
        await _repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loaded = await _repo.GetByIdAsync(42, TestContext.Current.CancellationToken);
        loaded.Should().NotBeNull();
        loaded!.SprintId.Should().Be(42);
        loaded.SprintName.Should().Be("Sprint 42");
    }

    // --- Slice 2: re-upsert overwrites — no duplicate row ---

    [Fact]
    public async Task Should_OverwriteSnapshot_When_AlreadyExists()
    {
        var initial = MakeSnapshot(42, state: "ACTIVE");
        await _repo.UpsertAsync(initial, TestContext.Current.CancellationToken);
        await _repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updated = FokusSprintSnapshot.CreateOrUpdate(42, "Sprint 42 Renamed", boardId: 2, syncedAt: DateTime.UtcNow, state: "CLOSED");
        await _repo.UpsertAsync(updated, TestContext.Current.CancellationToken);
        await _repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        var all = await _db.FokusSprintSnapshots.ToListAsync(TestContext.Current.CancellationToken);
        all.Should().HaveCount(1, "no duplicate rows — last-write-wins");
        all[0].SprintName.Should().Be("Sprint 42 Renamed");
        all[0].State.Should().Be("CLOSED");
        all[0].BoardId.Should().Be(2);
    }
}
