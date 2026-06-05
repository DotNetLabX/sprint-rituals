using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public class SprintRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Sprint, int>(db)
{
    public override IQueryable<Sprint> Query() =>
        Entity.Include(s => s.Memberships);

    public async Task<Sprint?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<Sprint>> GetAllAsync(CancellationToken ct = default) =>
        await Entity.OrderByDescending(s => s.EndDate).ToListAsync(ct);

    public async Task<List<Sprint>> GetAnalyticsSprintsAsync(CancellationToken ct = default) =>
        await Entity
            .Where(s => s.State == SprintState.Closed || s.State == SprintState.Active)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(ct);

    public async Task<Sprint?> GetSprintWithMembershipsAsync(int sprintId, CancellationToken ct = default) =>
        await Entity
            .Include(s => s.Memberships)
                .ThenInclude(m => m.Ticket)
                    .ThenInclude(t => t.Assignee)
            .SingleOrDefaultAsync(s => s.Id == sprintId, ct);

    public async Task<List<Sprint>> GetSprintsWithMembershipsAsync(List<int> sprintIds, CancellationToken ct = default) =>
        await Entity
            .Where(s => sprintIds.Contains(s.Id))
            .Include(s => s.Memberships)
                .ThenInclude(m => m.Ticket)
                    .ThenInclude(t => t.Assignee)
            .ToListAsync(ct);

    public async Task<List<SprintMembership>> GetAllAnalyticsSprintMembershipsAsync(CancellationToken ct = default) =>
        await DbContext.SprintMemberships
            .Where(sm => sm.Sprint.State == SprintState.Closed || sm.Sprint.State == SprintState.Active)
            .Include(sm => sm.Sprint)
            .Include(sm => sm.Ticket)
                .ThenInclude(t => t.Assignee)
            .ToListAsync(ct);

    public new async Task UpsertAsync(Sprint sprint, CancellationToken ct = default)
    {
        var existing = await Entity.SingleOrDefaultAsync(s => s.Id == sprint.Id, ct);
        if (existing is null)
        {
            Entity.Add(sprint);
        }
        else
        {
            existing.Name = sprint.Name;
            existing.StartDate = sprint.StartDate;
            existing.EndDate = sprint.EndDate;
            existing.BoardId = sprint.BoardId;
            existing.BoardName = sprint.BoardName;
            existing.State = sprint.State;
            existing.SyncedAt = sprint.SyncedAt;
            existing.Goal = sprint.Goal;
        }
    }

    public async Task UpsertMembershipsAsync(int sprintId, List<SprintMembership> memberships, CancellationToken ct = default)
    {
        var existing = await DbContext.SprintMemberships
            .Where(sm => sm.SprintId == sprintId)
            .ToListAsync(ct);

        DbContext.SprintMemberships.RemoveRange(existing);
        DbContext.SprintMemberships.AddRange(memberships);
    }
}
