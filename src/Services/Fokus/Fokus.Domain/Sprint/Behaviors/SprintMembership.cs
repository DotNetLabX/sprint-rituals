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
}
