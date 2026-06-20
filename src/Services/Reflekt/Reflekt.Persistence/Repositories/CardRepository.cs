using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.Repositories;

public sealed class CardRepository(ReflektDbContext db) : Repository<Card>(db)
{
    public override Task<Card?> GetByIdAsync(int id, CancellationToken ct)
        => Db.Cards
            .Include(c => c.EvidenceLinks)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Card>> GetByRetroSessionIdAsync(int retroSessionId, CancellationToken ct)
        => Db.Cards
            .Include(c => c.EvidenceLinks)
            .Where(c => c.RetroSessionId == retroSessionId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Card>> GetUnreviewedActionItemsByRetroSessionIdAsync(int retroSessionId, CancellationToken ct)
        => Db.Cards
            .Where(c => c.RetroSessionId == retroSessionId
                && c.CardType == CardType.ActionItem
                && c.ReviewedInRetroSessionId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Card>> GetActionItemsReviewedInRetroSessionAsync(int reviewingRetroSessionId, CancellationToken ct)
        => Db.Cards
            .Where(c => c.ReviewedInRetroSessionId == reviewingRetroSessionId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountActionItemsByRetroSessionIdAsync(int retroSessionId, CancellationToken ct)
        => Db.Cards
            .CountAsync(c => c.RetroSessionId == retroSessionId && c.CardType == CardType.ActionItem, ct);

    public void Remove(Card card) => Db.Cards.Remove(card);
}
