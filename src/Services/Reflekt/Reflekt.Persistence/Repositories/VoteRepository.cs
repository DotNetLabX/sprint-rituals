using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence.Repositories;

public sealed class VoteRepository(ReflektDbContext db) : Repository<Vote>(db)
{
    public Task<IDbContextTransaction> BeginSerializableAsync(CancellationToken ct)
        => Db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

    public Task<int> CountByParticipantAsync(int participantId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.ParticipantId == participantId, ct);

    public Task<int> CountByGroupAsync(int groupId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.GroupId == groupId, ct);

    public Task<int> CountByParticipantAndGroupAsync(int participantId, int groupId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.ParticipantId == participantId && v.GroupId == groupId, ct);

    public Task<Vote?> GetOneByParticipantAndGroupAsync(int participantId, int groupId, CancellationToken ct)
        => Db.Votes.FirstOrDefaultAsync(v => v.ParticipantId == participantId && v.GroupId == groupId, ct);

    public Task<List<VoteDistributionEntry>> GetVoteDistributionAsync(int participantId, CancellationToken ct)
        => Db.Votes
            .Where(v => v.ParticipantId == participantId)
            .GroupBy(v => v.GroupId)
            .Select(g => new VoteDistributionEntry(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<Dictionary<int, int>> GetVoteCountsByGroupIdsAsync(IEnumerable<int> groupIds, CancellationToken ct)
    {
        var ids = groupIds.ToList();
        return await Db.Votes
            .Where(v => ids.Contains(v.GroupId))
            .GroupBy(v => v.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);
    }

    public Task<List<int>> GetParticipantIdsWithVotesForRetroSessionAsync(int retroSessionId, CancellationToken ct)
        => Db.Votes
            .Where(v => Db.CardGroups.Any(g => g.Id == v.GroupId && g.RetroSessionId == retroSessionId))
            .Select(v => v.ParticipantId)
            .Distinct()
            .ToListAsync(ct);

    public void Remove(Vote vote) => Db.Votes.Remove(vote);
}

public sealed record VoteDistributionEntry(int GroupId, int Count);
