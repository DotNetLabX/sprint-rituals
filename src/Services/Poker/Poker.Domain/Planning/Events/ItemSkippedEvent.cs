namespace Poker.Domain.Planning.Events;

/// <summary>Raised when the SM skips an item (Rule 9 — manual only). Triggers ItemSkipped broadcast.</summary>
public sealed record ItemSkippedEvent(Guid SessionId, Guid ItemId) : IDomainEvent;
