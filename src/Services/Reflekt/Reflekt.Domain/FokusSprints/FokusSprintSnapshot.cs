namespace Reflekt.Domain.FokusSprints;

/// <summary>
/// Read-only projection of a Fokus sprint, refreshed by the <c>SprintSynced</c> integration event (ADR-019).
/// Holds exactly the fields carried by <c>SprintSynced</c>: no rich metrics (those are RA4 / gRPC ISprintAnalyticsService).
/// Natural key: <see cref="SprintId"/> (the Fokus sprint ID). Last-write-wins on re-sync.
/// </summary>
public sealed class FokusSprintSnapshot
{
    private FokusSprintSnapshot() { }

    /// <summary>Creates a new snapshot or updates an existing one from a SprintSynced event payload.</summary>
    public static FokusSprintSnapshot CreateOrUpdate(
        int sprintId,
        string sprintName,
        int boardId,
        DateTime syncedAt,
        string state)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sprintId, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(sprintName);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        return new FokusSprintSnapshot
        {
            SprintId = sprintId,
            SprintName = sprintName,
            BoardId = boardId,
            SyncedAt = syncedAt,
            State = state,
        };
    }

    /// <summary>Fokus sprint ID — natural key, ValueGeneratedNever.</summary>
    public int SprintId { get; private set; }
    public string SprintName { get; private set; } = default!;
    public int BoardId { get; private set; }
    public DateTime SyncedAt { get; private set; }
    public string State { get; private set; } = default!;

    /// <summary>Overwrites this snapshot with fresh data from a SprintSynced event.</summary>
    public void Update(string sprintName, int boardId, DateTime syncedAt, string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sprintName);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        SprintName = sprintName;
        BoardId = boardId;
        SyncedAt = syncedAt;
        State = state;
    }
}
