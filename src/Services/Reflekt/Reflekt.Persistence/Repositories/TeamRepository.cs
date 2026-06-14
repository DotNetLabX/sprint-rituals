using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.Repositories;

public sealed class TeamRepository(ReflektDbContext db) : Repository<Team>(db)
{
    public Task<int> CountByOwnerIdAsync(int ownerId, CancellationToken ct)
        => Db.Teams.CountAsync(t => t.OwnerId == ownerId, ct);

    public Task<bool> ExistsByOwnerAndNameAsync(int ownerId, string name, CancellationToken ct)
        => Db.Teams.AnyAsync(
            t => t.OwnerId == ownerId && EF.Functions.Collate(t.Name, "NOCASE") == name,
            ct);

    public async Task<List<TeamSummaryDto>> GetTeamSummariesByOwnerAsync(int ownerId, CancellationToken ct)
        => await Db.Teams
            .Where(t => t.OwnerId == ownerId)
            .Select(t => new TeamSummaryDto(
                t.Id,
                t.Name,
                t.CreatedAt,
                Db.Sprints.Count(s => s.TeamId == t.Id),
                Db.Sprints.Where(s => s.TeamId == t.Id).Max(s => (DateTime?)s.CreatedAt),
                Db.Sprints.Any(s => s.TeamId == t.Id && s.State == SprintState.Active)))
            .ToListAsync(ct);

    public async Task DeleteTeamWithCascadeAsync(int teamId, CancellationToken ct)
    {
        await using var tx = await Db.Database.BeginTransactionAsync(ct);

        var sprintIds = await Db.Sprints
            .Where(s => s.TeamId == teamId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (sprintIds.Count > 0)
        {
            var groupIds = await Db.CardGroups
                .Where(g => sprintIds.Contains(g.SprintId))
                .Select(g => g.Id)
                .ToListAsync(ct);

            if (groupIds.Count > 0)
                await Db.Votes.Where(v => groupIds.Contains(v.GroupId)).ExecuteDeleteAsync(ct);

            var cardIds = await Db.Cards
                .Where(c => sprintIds.Contains(c.SprintId))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (cardIds.Count > 0)
                await Db.EvidenceLinks.Where(e => cardIds.Contains(e.CardId)).ExecuteDeleteAsync(ct);

            await Db.Cards.Where(c => sprintIds.Contains(c.SprintId)).ExecuteDeleteAsync(ct);
            await Db.CardGroups.Where(g => sprintIds.Contains(g.SprintId)).ExecuteDeleteAsync(ct);
            await Db.Participants.Where(p => sprintIds.Contains(p.SprintId)).ExecuteDeleteAsync(ct);
            await Db.Sprints.Where(s => s.TeamId == teamId).ExecuteDeleteAsync(ct);
        }

        await Db.Teams.Where(t => t.Id == teamId).ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);
    }
}

public sealed record TeamSummaryDto(
    int Id,
    string Name,
    DateTime CreatedAt,
    int SprintCount,
    DateTime? LastSprintDate,
    bool HasActiveSprint);
