namespace Poker.Domain.Planning.Votes;

/// <summary>
/// A participant's vote on an estimation item.
/// Rule 21: at most one vote per participant per item — cast via EstimationItem.CastVote (upsert).
/// </summary>
public sealed class Vote : Entity<Guid>
{
    private Vote() { }

    public required Guid ItemId { get; init; }
    public required Guid ParticipantId { get; init; }

    /// <summary>Fibonacci value including "?" (abstain). Validated at CastVote time.</summary>
    public string Value { get; private set; } = default!;

    public DateTime SubmittedAt { get; private set; }

    internal static Vote Cast(Guid itemId, Guid participantId, string value, DateTime submittedAt)
    {
        if (!FibonacciScale.Values.Contains(value))
            throw new ArgumentException($"'{value}' is not a valid Fibonacci estimate.", nameof(value));

        return new Vote
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            ParticipantId = participantId,
            Value = value,
            SubmittedAt = submittedAt,
        };
    }

    /// <summary>Replaces the value on an existing vote (pre-reveal change, Rule 21).</summary>
    internal void ChangeValue(string newValue, DateTime changedAt)
    {
        if (!FibonacciScale.Values.Contains(newValue))
            throw new ArgumentException($"'{newValue}' is not a valid Fibonacci estimate.", nameof(newValue));

        Value = newValue;
        SubmittedAt = changedAt;
    }
}
