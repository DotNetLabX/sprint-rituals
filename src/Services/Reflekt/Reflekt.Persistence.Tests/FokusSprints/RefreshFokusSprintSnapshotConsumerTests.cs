using AwesomeAssertions;
using Blocks.Contracts.IntegrationEvents;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reflekt.API.Features.FokusSprints.Consumers;
using Reflekt.Persistence;
using Reflekt.Persistence.Repositories;
using Xunit;

namespace Reflekt.Persistence.Tests.FokusSprints;

/// <summary>
/// Tests the consumer's idempotent upsert behavior (ADR-019).
/// Uses a shared SqliteConnection so all scoped DbContext instances see the same in-memory DB.
/// </summary>
public class RefreshFokusSprintSnapshotConsumerTests : IAsyncDisposable
{
    private readonly SqliteConnection _sharedConnection;
    private readonly ServiceProvider _provider;
    private readonly ITestHarness _harness;

    public RefreshFokusSprintSnapshotConsumerTests()
    {
        // Shared connection keeps the in-memory DB alive across DI scopes
        _sharedConnection = new SqliteConnection("Data Source=:memory:");
        _sharedConnection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<ReflektDbContext>(opts =>
            opts.UseSqlite(_sharedConnection));

        services.AddScoped<FokusSprintSnapshotRepository>();

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<RefreshFokusSprintSnapshotOnSprintSyncedConsumer>();
        });

        _provider = services.BuildServiceProvider();

        // Create schema using the shared connection
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ReflektDbContext>().Database.EnsureCreated();

        _harness = _provider.GetRequiredService<ITestHarness>();
    }

    public async ValueTask DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
        await _sharedConnection.DisposeAsync();
    }

    // --- Slice 1: SprintSynced → snapshot row created ---

    [Fact]
    public async Task Should_CreateSnapshot_When_SprintSyncedReceived()
    {
        await _harness.Start();

        await _harness.Bus.Publish(new SprintSynced(42, "Sprint 42", 1, DateTime.UtcNow, "ACTIVE"),
            TestContext.Current.CancellationToken);

        (await _harness.Consumed.Any<SprintSynced>(TestContext.Current.CancellationToken)).Should().BeTrue();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        var snapshot = await db.FokusSprintSnapshots
            .FirstOrDefaultAsync(s => s.SprintId == 42, TestContext.Current.CancellationToken);

        snapshot.Should().NotBeNull();
        snapshot!.SprintName.Should().Be("Sprint 42");
        snapshot.State.Should().Be("ACTIVE");
    }

    // --- Slice 2: re-consume with changed State → overwrite, no duplicate ---

    [Fact]
    public async Task Should_OverwriteSnapshot_When_SprintSyncedReceivedAgain()
    {
        await _harness.Start();

        await _harness.Bus.Publish(new SprintSynced(55, "Sprint 55", 2, DateTime.UtcNow, "ACTIVE"),
            TestContext.Current.CancellationToken);
        await _harness.Bus.Publish(new SprintSynced(55, "Sprint 55 Done", 2, DateTime.UtcNow, "CLOSED"),
            TestContext.Current.CancellationToken);

        // Wait for the second message (CLOSED) to be consumed
        await _harness.Consumed.Any<SprintSynced>(x =>
            x.Context!.Message.SprintId == 55 && x.Context.Message.State == "CLOSED",
            TestContext.Current.CancellationToken);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        var all = await db.FokusSprintSnapshots
            .Where(s => s.SprintId == 55)
            .ToListAsync(TestContext.Current.CancellationToken);

        all.Should().HaveCount(1, "last-write-wins: only one row per SprintId");
        all[0].State.Should().Be("CLOSED");
        all[0].SprintName.Should().Be("Sprint 55 Done");
    }
}
