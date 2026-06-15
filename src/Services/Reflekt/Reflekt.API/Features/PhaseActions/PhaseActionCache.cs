using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence;

namespace Reflekt.API.Features.PhaseActions;

public sealed class PhaseActionCache
{
    private readonly HashSet<(RetroPhase, RetroAction)> _allowed;

    public PhaseActionCache(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        var rows = db.PhaseActions.AsNoTracking().ToList();
        _allowed = rows.Select(r => (r.Phase, r.Action)).ToHashSet();
    }

    public bool IsAllowed(RetroPhase phase, RetroAction action) =>
        _allowed.Contains((phase, action));

    public Dictionary<RetroPhase, List<RetroAction>> GetMatrix() =>
        _allowed
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToList());
}

