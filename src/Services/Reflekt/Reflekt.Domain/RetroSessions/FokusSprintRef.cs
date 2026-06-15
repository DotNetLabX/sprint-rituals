namespace Reflekt.Domain.RetroSessions;

/// <summary>
/// Links a <see cref="RetroSession"/> to a Fokus sprint (ADR-018).
/// A retro session references 1..N Fokus sprints — this is the link record.
/// Composite key: (RetroSessionId, FokusSprintId).
/// </summary>
public sealed class FokusSprintRef
{
    private FokusSprintRef() { }

    public FokusSprintRef(int fokusSprintId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fokusSprintId, 1);
        FokusSprintId = fokusSprintId;
    }

    public int RetroSessionId { get; private set; }
    public int FokusSprintId { get; private set; }
}
