using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public class DeveloperRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Developer, string>(db)
{
    public async Task<Developer?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await Entity.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<List<Developer>> GetAllAsync(CancellationToken ct = default) =>
        await Entity.OrderBy(d => d.DisplayName).ToListAsync(ct);

    public async Task<List<string>> GetDistinctSubTeamsAsync(CancellationToken ct = default) =>
        await Entity
            .Where(d => d.SubTeam != null)
            .Select(d => d.SubTeam!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

    public async Task<List<Developer>> GetActiveDevelopersAsync(CancellationToken ct = default) =>
        await Entity
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayName)
            .ToListAsync(ct);

    public new async Task UpsertAsync(Developer developer, CancellationToken ct = default)
    {
        var existing = Entity.Local.SingleOrDefault(d => d.Id == developer.Id)
            ?? await Entity.SingleOrDefaultAsync(d => d.Id == developer.Id, ct);
        if (existing is null)
        {
            Entity.Add(developer);
        }
        else
        {
            existing.DisplayName = developer.DisplayName;
            existing.AvatarUrl = developer.AvatarUrl;
            existing.IsActive = developer.IsActive;
        }
    }

    public async Task<DeveloperSprintCapacity?> GetCapacityAsync(string accountId, int sprintId, CancellationToken ct = default) =>
        await db.DeveloperSprintCapacities
            .SingleOrDefaultAsync(c => c.DeveloperAccountId == accountId && c.SprintId == sprintId, ct);

    public async Task<List<DeveloperSprintCapacity>> GetCapacitiesForDeveloperAsync(string accountId, int? sprintId, CancellationToken ct = default)
    {
        var query = db.DeveloperSprintCapacities
            .Where(c => c.DeveloperAccountId == accountId);

        if (sprintId.HasValue)
            query = query.Where(c => c.SprintId == sprintId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task UpsertCapacityAsync(string accountId, int sprintId, int capacityPercent, CancellationToken ct = default)
    {
        var existing = await db.DeveloperSprintCapacities
            .SingleOrDefaultAsync(c => c.DeveloperAccountId == accountId && c.SprintId == sprintId, ct);

        if (existing is null)
        {
            db.DeveloperSprintCapacities.Add(new DeveloperSprintCapacity
            {
                DeveloperAccountId = accountId,
                SprintId = sprintId,
                CapacityPercent = capacityPercent
            });
        }
        else
        {
            existing.CapacityPercent = capacityPercent;
        }
    }

    public async Task<List<DeveloperSprintCapacity>> GetCapacitiesForSprintsAsync(List<int> sprintIds, CancellationToken ct = default) =>
        await db.DeveloperSprintCapacities
            .Where(c => sprintIds.Contains(c.SprintId))
            .ToListAsync(ct);

    public async Task<Developer?> UpdateTeamConfigAsync(
        string accountId,
        string? role,
        int? defaultCapacityPercent,
        string? subTeam,
        bool subTeamProvided,
        bool? isActive,
        CancellationToken ct = default)
    {
        var developer = await Entity.SingleOrDefaultAsync(d => d.Id == accountId, ct);
        if (developer is null) return null;
        if (role is not null) developer.Role = role;
        if (defaultCapacityPercent.HasValue) developer.DefaultCapacityPercent = defaultCapacityPercent.Value;
        if (subTeamProvided) developer.SubTeam = subTeam;
        if (isActive.HasValue) developer.IsActive = isActive.Value;
        return developer;
    }

}
