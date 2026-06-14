using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.Repositories;

public sealed class CardRepository(ReflektDbContext db) : Repository<Card>(db)
{
    public override Task<Card?> GetByIdAsync(int id, CancellationToken ct)
        => Db.Cards
            .Include(c => c.EvidenceLinks)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Card>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards
            .Include(c => c.EvidenceLinks)
            .Where(c => c.SprintId == sprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Card>> GetUnreviewedActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards
            .Where(c => c.SprintId == sprintId
                && c.CardType == CardType.ActionItem
                && c.ReviewedInSprintId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Card>> GetActionItemsReviewedInSprintAsync(int reviewingSprintId, CancellationToken ct)
        => Db.Cards
            .Where(c => c.ReviewedInSprintId == reviewingSprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards
            .CountAsync(c => c.SprintId == sprintId && c.CardType == CardType.ActionItem, ct);

    public void Remove(Card card) => Db.Cards.Remove(card);
}
