namespace Fokus.Domain;

public partial class SprintMembership
{
    /// <summary>
    /// Returns the effective SP for this membership:
    /// - If StoryPoints has a value and is > 0, returns StoryPoints.
    /// - If ticket is a Bug and defaultSpPerBug > 0, returns defaultSpPerBug as decimal.
    /// - Otherwise returns null.
    /// </summary>
    public decimal? GetEffectiveSp(int defaultSpPerBug)
    {
        if (StoryPoints.HasValue && StoryPoints.Value > 0)
            return StoryPoints;

        if (Ticket?.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;

        return null;
    }

    public static SprintMembership FromJira(JiraIssue dto, Sprint sprint, bool forcedNotCommitted = false, int planningWindowDays = 2)
    {
        var sprintIdStr = sprint.Id.ToString();

        DateTime? addedAt = null;
        DateTime? removedAt = null;

        var histories = dto.Changelog?.Histories;
        if (histories is not null)
        foreach (var history in histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field is "Sprint" or "sprint"))
            {
                var toSprints = item.To?.Split(',').Select(s => s.Trim()) ?? [];
                var fromSprints = item.From?.Split(',').Select(s => s.Trim()) ?? [];

                var appearsInTo = toSprints.Any(s => s == sprintIdStr);
                var appearsInFrom = fromSprints.Any(s => s == sprintIdStr);

                if (appearsInTo && !appearsInFrom && addedAt is null)
                    addedAt = history.Created;

                if (appearsInFrom && !appearsInTo && removedAt is null)
                    removedAt = history.Created;
            }
        }

        var effectiveAddedAt = addedAt ?? sprint.StartDate;
        var planningCutoff = sprint.StartDate.AddDays(planningWindowDays);
        var wasCommitted = !forcedNotCommitted && effectiveAddedAt <= planningCutoff;

        return new SprintMembership
        {
            SprintId = sprint.Id,
            TicketId = dto.Key,
            AddedAt = effectiveAddedAt,
            RemovedAt = removedAt,
            WasCommitted = wasCommitted,
            FinalStatus = dto.Fields.Status?.Name ?? "Unknown",
            StoryPoints = dto.Fields.StoryPoints
        };
    }
}
