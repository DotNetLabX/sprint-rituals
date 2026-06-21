namespace Poker.Domain.Planning.Events;

/// <summary>
/// Raised when the SM decides the final estimate and (Sprint-bound) write-back is confirmed.
/// Triggers EstimateDecided broadcast.
/// </summary>
public sealed record EstimateDecidedEvent(Guid SessionId, Guid ItemId, string FinalEstimate) : IDomainEvent;
