using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;
using Reflekt.Persistence;
using Reflekt.Persistence.Repositories;
using Xunit;

namespace Reflekt.Persistence.Tests.RetroSessions;

/// <summary>
/// Round-trip tests proving the ComplexProperty config for RetroColumn (×3) and
/// TimerDefaults survives save → reload, including the ColumnColor conversion.
/// Uses a real SQLite in-memory connection (not the EF in-memory provider) so
/// value converters and defaults are exercised.
/// </summary>
public class RetroSessionColumnTimerPersistenceTests : IDisposable
{
    // Keep a raw connection open so the in-memory DB lives for the duration of the test.
    // A second ReflektDbContext sharing this connection bypasses the first-level cache.
    private readonly SqliteConnection _connection;
    private readonly ReflektDbContext _db;
    private readonly RetroSessionRepository _retroRepo;

    public RetroSessionColumnTimerPersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ReflektDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ReflektDbContext(options);
        _db.Database.EnsureCreated();

        _retroRepo = new RetroSessionRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private ReflektDbContext FreshContext()
    {
        // Shares the same open connection → same in-memory DB, but a new change-tracker
        // so no first-level cache interference.
        var options = new DbContextOptionsBuilder<ReflektDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ReflektDbContext(options);
    }

    private async Task<int> SeedTeamAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var team = Team.Create("Team Alpha", ownerId: 1);
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        return team.Id;
    }

    // --- Round-trip 1: custom columns + timers survive save → reload ---

    [Fact]
    public async Task Should_PersistAndReload_CustomColumnsAndTimers()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamId = await SeedTeamAsync();

        var session = RetroSession.Create(
            "Custom Retro", teamId, fokusSprintIds: [1],
            column1Name: "Wins", column1Color: "blue",
            column2Name: "Losses", column2Color: "red",
            column3Name: "Tries", column3Color: "amber",
            writingTimerDefault: 8, votingTimerDefault: 4, discussingTimerDefault: 12);

        await _retroRepo.AddAsync(session, ct);
        await _retroRepo.SaveChangesAsync(ct);

        await using var fresh = FreshContext();
        var reloaded = await fresh.RetroSessions
            .Include(s => s.FokusSprintRefs)
            .SingleAsync(s => s.Id == session.Id, ct);

        reloaded.Column1.Name.Should().Be("Wins");
        reloaded.Column1.Color.Value.Should().Be("blue");
        reloaded.Column2.Name.Should().Be("Losses");
        reloaded.Column2.Color.Value.Should().Be("red");
        reloaded.Column3.Name.Should().Be("Tries");
        reloaded.Column3.Color.Value.Should().Be("amber");
        reloaded.Timers.Writing.Should().Be(8);
        reloaded.Timers.Voting.Should().Be(4);
        reloaded.Timers.Discussing.Should().Be(12);
    }

    // --- Round-trip 2: default columns + timers survive save → reload ---
    // This exercises HasDefaultValue on the renamed complex columns; the custom-values
    // case alone would not catch a default that was lost in the rename migration.

    [Fact]
    public async Task Should_PersistAndReload_DefaultColumnsAndTimers()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamId = await SeedTeamAsync();

        var session = RetroSession.Create("Default Retro", teamId, fokusSprintIds: [2]);

        await _retroRepo.AddAsync(session, ct);
        await _retroRepo.SaveChangesAsync(ct);

        await using var fresh = FreshContext();
        var reloaded = await fresh.RetroSessions
            .Include(s => s.FokusSprintRefs)
            .SingleAsync(s => s.Id == session.Id, ct);

        reloaded.Column1.Name.Should().Be("Went Well");
        reloaded.Column1.Color.Value.Should().Be("green");
        reloaded.Column2.Name.Should().Be("Didn't Go Well");
        reloaded.Column2.Color.Value.Should().Be("red");
        reloaded.Column3.Name.Should().Be("Start Doing");
        reloaded.Column3.Color.Value.Should().Be("amber");
        reloaded.Timers.Writing.Should().Be(5);
        reloaded.Timers.Voting.Should().Be(3);
        reloaded.Timers.Discussing.Should().Be(10);
    }
}
