using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence;

namespace Reflekt.API.Features.PhaseActions;

public sealed class PhaseActionCache
{
    private readonly HashSet<(SprintPhase, SprintAction)> _allowed;

    public PhaseActionCache(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        var rows = db.PhaseActions.AsNoTracking().ToList();
        _allowed = rows.Select(r => (r.Phase, r.Action)).ToHashSet();
    }

    public bool IsAllowed(SprintPhase phase, SprintAction action) =>
        _allowed.Contains((phase, action));

    public Dictionary<SprintPhase, List<SprintAction>> GetMatrix() =>
        _allowed
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToList());
}
