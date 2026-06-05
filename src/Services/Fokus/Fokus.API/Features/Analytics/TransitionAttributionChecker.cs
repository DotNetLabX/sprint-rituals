namespace Fokus.API.Features.Analytics;

/// <summary>
/// Utility that encapsulates transition-timestamp-based sprint scope attribution.
/// A ticket is "started" in the sprint where its first transition to CycleTimeStartStage (or beyond) occurred.
/// A ticket is "completed" in the sprint where its first transition to CycleTimeEndStage (or beyond) occurred.
/// Each transition timestamp falls in exactly one sprint — no double-counting.
/// </summary>
/// <remarks>
/// This is the counterpart to CompletionChecker, which handles position-based FinalStatus checks and is retained
/// for non-sprint-scope uses (EpicProgress progress tracking, ExcludedFromScope filtering).
/// TransitionAttributionChecker answers "did this ticket transition to a stage during this sprint?"
/// while CompletionChecker answers "is this status a completed status?" (snapshot-based).
/// </remarks>
public static class TransitionAttributionChecker
{
    /// <summary>
    /// Returns the ordered stage sequence and the index of the start boundary stage.
    /// </summary>
    /// <remarks>
    /// Fallback behavior (spec BR19 divergence from cycle time):
    /// - No CycleTimeStartStage configured: defaults to orderedStages[0] (first stage, including queue).
    ///   This captures all sprint engagement including queue entry — wider than cycle time measurement
    ///   which skips the queue by defaulting to the second workflow stage.
    /// - Start stage not found in ordered sequence: falls back to index 0.
    /// </remarks>
    public static (List<string> OrderedStages, int StartIndex) ResolveStartIndex(AppSettings settings)
    {
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        // BR19 fallback: first stage (index 0), not second stage like cycle time measurement
        var startStage = settings.CycleTimeStartStage
            ?? (orderedStages.Count > 0 ? orderedStages[0] : null);

        if (startStage is null)
            return (orderedStages, 0);

        var startIndex = orderedStages.FindIndex(s => string.Equals(s, startStage, StringComparison.OrdinalIgnoreCase));

        // Not found: fall back to 0
        if (startIndex < 0)
            startIndex = 0;

        return (orderedStages, startIndex);
    }

    /// <summary>
    /// Returns the index of the end boundary stage in the ordered stage sequence.
    /// </summary>
    /// <remarks>
    /// Fallback behavior:
    /// - No CycleTimeEndStage configured: defaults to first done status.
    /// - End stage not found in ordered sequence: falls back to first done status position.
    /// - No done statuses at all: returns -1 (nothing completes).
    /// </remarks>
    public static int ResolveEndIndex(AppSettings settings, List<string> orderedStages)
    {
        var endStage = settings.CycleTimeEndStage
            ?? (settings.DoneStatuses.Count > 0 ? settings.DoneStatuses[0] : null);

        if (endStage is null)
            return -1;

        var endIndex = orderedStages.FindIndex(s => string.Equals(s, endStage, StringComparison.OrdinalIgnoreCase));

        if (endIndex < 0 && settings.DoneStatuses.Count > 0)
        {
            var fallbackEndStage = settings.DoneStatuses[0];
            endIndex = orderedStages.FindIndex(s => string.Equals(s, fallbackEndStage, StringComparison.OrdinalIgnoreCase));
        }

        return endIndex; // -1 if still not found
    }

    /// <summary>
    /// Returns the index of a status in the ordered stage sequence, or -1 if not found. Case-insensitive.
    /// </summary>
    public static int GetStageIndex(string status, List<string> orderedStages) =>
        orderedStages.FindIndex(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Checks if a ticket has a qualifying start transition within [sprintStart, sprintEnd].
    /// Returns (true, earliestQualifyingTimestamp) if started, (false, null) otherwise.
    /// A transition qualifies when GetStageIndex(ToStatus) >= startIndex.
    /// </summary>
    public static (bool IsStarted, DateTime? StartedAt) IsStartedInSprint(
        string ticketId,
        List<StatusTransition> transitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex)
    {
        if (startIndex < 0)
            return (false, null);

        var qualifying = transitions
            .Where(t => t.TicketId == ticketId
                        && t.Timestamp >= sprintStart
                        && t.Timestamp <= sprintEnd
                        && GetStageIndex(t.ToStatus, orderedStages) >= startIndex)
            .OrderBy(t => t.Timestamp)
            .FirstOrDefault();

        if (qualifying is null)
            return (false, null);

        return (true, qualifying.Timestamp);
    }

    /// <summary>
    /// Checks if a ticket has a qualifying completion transition within [sprintStart, sprintEnd].
    /// Returns (true, earliestQualifyingTimestamp) if completed, (false, null) otherwise.
    /// A transition qualifies when GetStageIndex(ToStatus) >= endIndex.
    /// </summary>
    public static (bool IsCompleted, DateTime? CompletedAt) IsCompletedInSprint(
        string ticketId,
        List<StatusTransition> transitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int endIndex)
    {
        if (endIndex < 0)
            return (false, null);

        var qualifying = transitions
            .Where(t => t.TicketId == ticketId
                        && t.Timestamp >= sprintStart
                        && t.Timestamp <= sprintEnd
                        && GetStageIndex(t.ToStatus, orderedStages) >= endIndex)
            .OrderBy(t => t.Timestamp)
            .FirstOrDefault();

        if (qualifying is null)
            return (false, null);

        return (true, qualifying.Timestamp);
    }

    /// <summary>
    /// Returns true if the ticket is an "added" scope change.
    /// Spec BR3: post-planning addition that entered the cycle.
    /// Both conditions must hold: AddedAt > planningCutoff AND isStarted.
    /// The caller is responsible for passing the correct cutoff value
    /// (sprint.StartDate.AddDays(planningWindowDays)).
    /// </summary>
    public static bool IsAddedInSprint(SprintMembership membership, bool isStarted, DateTime planningCutoff) =>
        membership.AddedAt > planningCutoff && isStarted;

    /// <summary>
    /// Returns true if the ticket qualifies as removed SP under planning-gated semantics (spec BR4).
    /// All three conditions must hold:
    /// 1. RemovedAt is not null and is after planningCutoff.
    /// 2. The ticket has a qualifying transition to CycleTimeStartStage (or beyond)
    ///    with timestamp in [sprintStart, membership.RemovedAt] — it entered the cycle during the sprint before being removed.
    /// </summary>
    public static bool IsRemovedPostPlanning(
        SprintMembership membership,
        List<StatusTransition> ticketTransitions,
        DateTime sprintStart,
        DateTime planningCutoff,
        List<string> orderedStages,
        int startIndex)
    {
        if (membership.RemovedAt == null || membership.RemovedAt <= planningCutoff)
            return false;

        return ticketTransitions.Any(t =>
            t.Timestamp >= sprintStart
            && t.Timestamp <= membership.RemovedAt.Value
            && GetStageIndex(t.ToStatus, orderedStages) >= startIndex);
    }

    /// <summary>
    /// Returns true if the ticket is carry-over for this sprint.
    /// Spec BR12: started but not completed in the same sprint.
    /// </summary>
    public static bool IsCarryOver(bool isStarted, bool isCompleted) =>
        isStarted && !isCompleted;
}
