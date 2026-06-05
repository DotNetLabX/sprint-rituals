namespace Fokus.API.Features.Analytics;

public static class ExcludedDeveloperFilter
{
    /// <summary>
    /// Returns account IDs of developers who should be excluded from sprint analytics.
    /// Exclusion rule: effective capacity == 0 AND completed 0 tickets in the sprint (transition-based).
    /// </summary>
    public static HashSet<string> GetExcludedDeveloperIds(
        Sprint sprint,
        List<Developer> developers,
        List<DeveloperSprintCapacity> capacityRecords,
        List<StatusTransition> statusTransitions,
        AppSettings settings)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var capacityLookup = capacityRecords
            .Where(c => c.SprintId == sprint.Id)
            .ToDictionary(c => c.DeveloperAccountId, c => c.CapacityPercent);

        var excluded = new HashSet<string>();

        foreach (var developer in developers)
        {
            // Compute effective capacity: sprint-level override first, then developer default
            var effectiveCapacity = capacityLookup.TryGetValue(developer.Id, out var cap)
                ? cap
                : developer.DefaultCapacityPercent;

            if (effectiveCapacity > 0)
                continue;

            // 0% effective capacity — check if completed any ticket (transition-based)
            var completedAny = sprint.Memberships.Any(m =>
            {
                if (m.Ticket?.AssigneeId != developer.Id) return false;
                if (m.RemovedAt != null) return false;
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, statusTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return isCompleted;
            });

            if (!completedAny)
                excluded.Add(developer.Id);
        }

        return excluded;
    }
}
