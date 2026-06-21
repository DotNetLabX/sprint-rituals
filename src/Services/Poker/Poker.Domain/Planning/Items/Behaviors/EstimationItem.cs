using Poker.Domain.Planning.Votes;

namespace Poker.Domain.Planning.Items;

/// <summary>Behavior methods for EstimationItem. State machine: Voting → Revealed → Decided | Skipped.</summary>
public sealed partial class EstimationItem
{
    /// <summary>
    /// Factory: create a new item at Voting status.
    /// </summary>
    public static EstimationItem Create(
        Guid sessionId,
        string label,
        int order,
        string? ticketKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new EstimationItem
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Label = label,
            TicketKey = ticketKey,
            Status = ItemStatus.Voting,
            Order = order,
        };
    }

    /// <summary>
    /// Cast or replace a vote (Rule 21 — one vote per participant per item).
    /// Validates the Fibonacci scale. No-op replacement if the same value is re-submitted.
    /// Cannot vote after reveal (Rule 7).
    /// </summary>
    public Vote CastVote(Guid participantId, string value, DateTime submittedAt)
    {
        if (Status != ItemStatus.Voting)
            throw new InvalidOperationException("Cannot vote after votes have been revealed.");

        if (!FibonacciScale.Values.Contains(value))
            throw new ArgumentException($"'{value}' is not a valid Fibonacci value.", nameof(value));

        var existing = _votes.FirstOrDefault(v => v.ParticipantId == participantId);
        if (existing is not null)
        {
            // Replace (Rule 21: no duplicate rows)
            existing.ChangeValue(value, submittedAt);
            return existing;
        }

        var vote = Vote.Cast(Id, participantId, value, submittedAt);
        _votes.Add(vote);
        return vote;
    }

    /// <summary>
    /// Check whether all currently-connected voters (supplied by the hub) have voted.
    /// This is a pure query — the hub owns connection state and calls this to decide auto-reveal.
    /// Rule 7.
    /// </summary>
    public bool AllPresentVotersHaveVoted(IReadOnlySet<Guid> connectedVoterIds)
    {
        if (connectedVoterIds.Count == 0) return false;
        return connectedVoterIds.All(id => _votes.Any(v => v.ParticipantId == id));
    }

    /// <summary>
    /// Reveal all votes (auto or SM force-reveal). Transitions Voting → Revealed.
    /// Rule 7.
    /// </summary>
    public void Reveal()
    {
        if (Status != ItemStatus.Voting)
            throw new InvalidOperationException("Cannot reveal: item is not in Voting status.");

        Status = ItemStatus.Revealed;
    }

    /// <summary>
    /// Decide the final estimate (SM picks winner). Transitions Revealed → Decided.
    /// Rules 6, 8: "?" is never final; value must be one of the values actually cast.
    /// Sprint-bound: caller must confirm write-back before calling Decide (Rule 10).
    /// </summary>
    public void Decide(string value, DateTime decidedAt)
    {
        if (Status != ItemStatus.Revealed)
            throw new InvalidOperationException("Cannot decide: votes must be revealed first.");

        // Rule 6: "?" is never the final estimate
        if (value == FibonacciScale.Abstain)
            throw new ArgumentException("'?' cannot be the final estimate (Rule 6).", nameof(value));

        // Rule 8: value must be one of the values actually cast
        var castValues = _votes.Select(v => v.Value).ToHashSet(StringComparer.Ordinal);
        if (!castValues.Contains(value))
            throw new ArgumentException(
                $"'{value}' was not cast in this round. Choose from: {string.Join(", ", castValues)}.",
                nameof(value));

        // Rule 6 (extended): if there are no numeric votes, the round cannot be decided
        if (!castValues.Any(FibonacciScale.IsNumeric))
            throw new InvalidOperationException("No numeric votes were cast. The round can only be skipped (Rule 6).");

        FinalEstimate = value;
        DecidedAt = decidedAt;
        Status = ItemStatus.Decided;
    }

    /// <summary>
    /// Skip the item (SM only, Rule 9 — manual, never automatic). Transitions Voting|Revealed → Skipped.
    /// No estimate recorded; no write-back.
    /// </summary>
    public void Skip()
    {
        if (Status is ItemStatus.Decided or ItemStatus.Skipped)
            throw new InvalidOperationException($"Cannot skip an item that is already {Status}.");

        Status = ItemStatus.Skipped;
    }
}
