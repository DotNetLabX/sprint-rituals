namespace Poker.Domain.Planning.Events;

/// <summary>Raised when the SM closes the session. Triggers SessionClosed broadcast with summary stats.</summary>
public sealed record SessionClosedEvent(
    Guid SessionId,
    int ItemsEstimated,
    int ItemsSkipped,
    decimal TotalStoryPoints) : IDomainEvent;
