using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.FokusSprints;

namespace Reflekt.Persistence.Repositories;

/// <summary>
/// Repository for <see cref="FokusSprintSnapshot"/>.
/// Natural key is <c>SprintId</c> (the Fokus sprint id), not an auto-generated int.
/// Cannot extend <c>Repository{T}</c> which requires <c>IEntity{int}</c>.
/// </summary>
public sealed class FokusSprintSnapshotRepository(ReflektDbContext db)
{
    public Task<FokusSprintSnapshot?> GetByIdAsync(int sprintId, CancellationToken ct)
        => db.FokusSprintSnapshots.FirstOrDefaultAsync(s => s.SprintId == sprintId, ct);

    /// <summary>
    /// Inserts a new snapshot or overwrites an existing one (last-write-wins per ADR-019).
    /// Uses explicit per-column copy via <see cref="FokusSprintSnapshot.Update"/> to handle
    /// EF's private backing-field access correctly (avoids SetValues(object) shallow-copy pitfall).
    /// </summary>
    public async Task UpsertAsync(FokusSprintSnapshot snapshot, CancellationToken ct)
    {
        var existing = await GetByIdAsync(snapshot.SprintId, ct);
        if (existing is null)
            db.FokusSprintSnapshots.Add(snapshot);
        else
            existing.Update(snapshot.SprintName, snapshot.BoardId, snapshot.SyncedAt, snapshot.State);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}
