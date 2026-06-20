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

    /// <summary>
    /// Returns the <see cref="RetroSession"/> with its <see cref="RetroSession.FokusSprintRefs"/>
    /// eagerly loaded, or null if the id is unknown. Used by the sprint-context endpoint (RA4)
    /// to loop the retro's Fokus sprint references without a lazy-load round-trip.
    /// </summary>
    public Task<RetroSession?> GetWithFokusSprintsAsync(int retroSessionId, CancellationToken ct)
        => Db.RetroSessions
            .AsNoTracking()
            .Include(s => s.FokusSprintRefs)
            .FirstOrDefaultAsync(s => s.Id == retroSessionId, ct);
}

