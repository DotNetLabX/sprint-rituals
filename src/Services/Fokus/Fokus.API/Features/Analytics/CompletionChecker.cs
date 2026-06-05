namespace Fokus.API.Features.Analytics;

/// <summary>
/// Utility that encapsulates the boundary-driven completion rule.
/// A ticket is "completed" when its status is at or after the cycle time end stage
/// in the ordered stage sequence (WorkflowStages ++ DoneStatuses).
/// </summary>
public static class CompletionChecker
{
    /// <summary>
    /// Returns the set of statuses considered "completed" under the boundary-driven definition.
    /// </summary>
    /// <remarks>
    /// Fallback behavior:
    /// - No CycleTimeEndStage configured (null) AND DoneStatuses has entries: defaults to first done status — identical to pre-feature behavior.
    /// - No WorkflowStages configured (empty): ordered sequence = DoneStatuses only — identical to pre-feature behavior.
    /// - CycleTimeEndStage not in ordered sequence: falls back to first done status.
    /// - No done statuses at all: returns empty set.
    /// </remarks>
    public static HashSet<string> ResolveCompletedStatuses(AppSettings settings)
    {
        // Build ordered stage sequence: WorkflowStages ++ DoneStatuses
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        // Resolve end stage: CycleTimeEndStage ?? DoneStatuses[0]
        var endStage = settings.CycleTimeEndStage
            ?? (settings.DoneStatuses.Count > 0 ? settings.DoneStatuses[0] : null);

        if (endStage == null)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Find end stage index (case-insensitive)
        var endIdx = orderedStages.FindIndex(s => string.Equals(s, endStage, StringComparison.OrdinalIgnoreCase));

        // If end stage not found in sequence (BR5: cleared workflow stages after configuration),
        // fall back to first done status
        if (endIdx < 0 && settings.DoneStatuses.Count > 0)
        {
            var fallbackEndStage = settings.DoneStatuses[0];
            endIdx = orderedStages.FindIndex(s => string.Equals(s, fallbackEndStage, StringComparison.OrdinalIgnoreCase));
        }

        // If still not found (no done statuses at all): return empty set
        if (endIdx < 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // All statuses from end stage index to end of sequence
        return new HashSet<string>(
            orderedStages.Skip(endIdx),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if the status is in the completed set.
    /// Exists for readability and to match the old doneStatuses.Contains(status) call pattern.
    /// </summary>
    public static bool IsCompleted(string status, HashSet<string> completedStatuses) =>
        completedStatuses.Contains(status);
}
