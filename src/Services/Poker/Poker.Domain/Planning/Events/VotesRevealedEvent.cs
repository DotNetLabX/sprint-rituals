namespace Poker.Domain.Planning.Events;

/// <summary>
/// Raised when votes are revealed (auto or SM force). Carries the vote tally for broadcast.
/// </summary>
public sealed record VotesRevealedEvent(
    Guid SessionId,
    Guid ItemId,
    IReadOnlyList<VoteResult> Votes) : IDomainEvent;

public sealed record VoteResult(Guid ParticipantId, string DisplayName, string Value);
