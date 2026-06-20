using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;
using Reflekt.Persistence;
using Reflekt.Persistence.Repositories;
using Xunit;

namespace Reflekt.Persistence.Tests.Participants;

public class ParticipantRepositoryTests : IDisposable
{
    private readonly ReflektDbContext _db;
    private readonly ParticipantRepository _repo;

    public ParticipantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ReflektDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new ReflektDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _repo = new ParticipantRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    // Participant → RetroSession → Team is a real FK chain; seed the parents so the insert satisfies it.
    private async Task<int> SeedSessionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var team = Team.Create("Team", ownerId: 1);
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);

        var session = RetroSession.Create("Retro", team.Id, fokusSprintIds: [1]);
        _db.RetroSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session.Id;
    }

    private async Task SeedParticipantAsync(int retroSessionId, string displayName, int? personId)
    {
        var participant = Participant.Join(displayName, retroSessionId, personId);
        await _repo.AddAsync(participant, TestContext.Current.CancellationToken);
        await _repo.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FindByPersonIdAsync_Should_ReturnParticipant_When_PersonJoinedSession()
    {
        var ct = TestContext.Current.CancellationToken;
        var sessionId = await SeedSessionAsync();
        await SeedParticipantAsync(sessionId, "Ada Lovelace", personId: 42);

        var found = await _repo.FindByPersonIdAsync(sessionId, 42, ct);

        found.Should().NotBeNull();
        found!.PersonId.Should().Be(42);
        found.DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task FindByPersonIdAsync_Should_ReturnNull_When_PersonOrSessionDoesNotMatch()
    {
        var ct = TestContext.Current.CancellationToken;
        var sessionId = await SeedSessionAsync();
        await SeedParticipantAsync(sessionId, "Ada Lovelace", personId: 42);

        // Same session, different person → miss; and same person, non-existent session → miss.
        // Both predicates must be ANDed for the idempotent-rejoin lookup to be correct.
        var differentPerson = await _repo.FindByPersonIdAsync(sessionId, 999, ct);
        var differentSession = await _repo.FindByPersonIdAsync(sessionId + 1000, 42, ct);

        differentPerson.Should().BeNull();
        differentSession.Should().BeNull();
    }
}
