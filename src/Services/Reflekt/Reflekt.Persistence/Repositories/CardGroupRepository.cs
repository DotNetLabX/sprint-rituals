using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.CardGroups;

namespace Reflekt.Persistence.Repositories;

public sealed class CardGroupRepository(ReflektDbContext db) : Repository<CardGroup>(db)
{
    public Task<List<CardGroup>> GetByRetroSessionIdAsync(int retroSessionId, CancellationToken ct)
        => Db.CardGroups
            .Where(g => g.RetroSessionId == retroSessionId)
            .ToListAsync(ct);

    public Task<List<CardGroup>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        return Db.CardGroups
            .Where(g => idList.Contains(g.Id))
            .ToListAsync(ct);
    }

    public Task AddRangeAsync(IEnumerable<CardGroup> groups, CancellationToken ct)
        => Db.CardGroups.AddRangeAsync(groups, ct);

    public void Remove(CardGroup group) => Db.CardGroups.Remove(group);
}
