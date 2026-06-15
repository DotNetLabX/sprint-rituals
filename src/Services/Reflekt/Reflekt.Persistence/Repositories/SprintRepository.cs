using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.Persistence.Repositories;

public sealed class RetroSessionRepository(ReflektDbContext db) : Repository<RetroSession>(db)
{
    public Task<List<RetroSession>> GetByTeamIdAsync(int teamId, CancellationToken ct)
        => Db.RetroSessions
            .Where(s => s.TeamId == teamId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public Task<RetroSession?> GetMostRecentClosedByTeamIdAsync(int teamId, CancellationToken ct)
        => Db.RetroSessions
            .Where(s => s.TeamId == teamId && s.State == RetroSessionState.Closed)
            .OrderByDescending(s => s.ClosedAt)
            .FirstOrDefaultAsync(ct);
}

