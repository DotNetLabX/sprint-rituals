using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Sprints;

namespace Reflekt.Persistence.Repositories;

public sealed class SprintRepository(ReflektDbContext db) : Repository<Sprint>(db)
{
    public Task<List<Sprint>> GetByTeamIdAsync(int teamId, CancellationToken ct)
        => Db.Sprints
            .Where(s => s.TeamId == teamId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public Task<Sprint?> GetMostRecentClosedByTeamIdAsync(int teamId, CancellationToken ct)
        => Db.Sprints
            .Where(s => s.TeamId == teamId && s.State == SprintState.Closed)
            .OrderByDescending(s => s.ClosedAt)
            .FirstOrDefaultAsync(ct);
}
