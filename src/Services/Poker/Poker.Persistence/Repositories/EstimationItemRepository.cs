using Poker.Domain.Planning.Items;

namespace Poker.Persistence.Repositories;

public class EstimationItemRepository(PokerDbContext db)
    : RepositoryBase<PokerDbContext, EstimationItem, Guid>(db)
{
    public async Task<EstimationItem?> GetByIdWithVotesAsync(Guid id, CancellationToken ct = default)
        => await Entity
            .Include(i => i.Votes)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
}
