namespace Poker.Persistence.Repositories;

public class PlanningSessionRepository(PokerDbContext db)
    : RepositoryBase<PokerDbContext, PlanningSession, Guid>(db)
{
    public async Task<PlanningSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Entity
            .Include(s => s.Participants)
            .Include(s => s.Items)
                .ThenInclude(i => i.Votes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}
