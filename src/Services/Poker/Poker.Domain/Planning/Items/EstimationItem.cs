using Poker.Domain.Planning.Votes;

namespace Poker.Domain.Planning.Items;

/// <summary>
/// An item being estimated in a planning session. State machine: Voting → Revealed → Decided | Skipped.
/// In Sprint-bound mode the TicketKey drives write-back to Jira.
/// </summary>
public sealed partial class EstimationItem : Entity<Guid>
{
    private EstimationItem() { }

    public required Guid SessionId { get; init; }

    /// <summary>
    /// Human label: Jira ticket key + summary (Sprint-bound) or SM-entered title (Free).
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Jira ticket key (Sprint-bound only). Drives write-back and uniqueness per session (Rule 19).
    /// Null for Free-mode items.
    /// </summary>
    public string? TicketKey { get; init; }

    public ItemStatus Status { get; private set; } = ItemStatus.Voting;

    /// <summary>Agreed estimate; set on Decide (never "?").</summary>
    public string? FinalEstimate { get; private set; }

    public int Order { get; init; }

    public DateTime? DecidedAt { get; private set; }

    private readonly List<Vote> _votes = [];
    public IReadOnlyList<Vote> Votes => _votes.AsReadOnly();
}
