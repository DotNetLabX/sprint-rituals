namespace Blocks.Contracts.IntegrationEvents;

/// <summary>
/// Published after a sprint is successfully synced from Jira. Carries all data a consumer needs
/// to build a local FokusSprintSnapshot projection without calling back to Fokus.
/// </summary>
/// <param name="SprintId">Fokus sprint ID.</param>
/// <param name="SprintName">Sprint display name.</param>
/// <param name="BoardId">Jira board ID.</param>
/// <param name="SyncedAt">Timestamp of the sync that produced this event.</param>
/// <param name="State">Sprint state string — "Active", "Closed", or "Future" (from SprintState enum).</param>
public record SprintSynced(
    int SprintId,
    string SprintName,
    int BoardId,
    DateTime SyncedAt,
    string State);
