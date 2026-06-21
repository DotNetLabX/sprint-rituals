namespace Poker.Domain.Planning.Events;

/// <summary>Raised when the SM selects an item to estimate. Triggers ItemSelected broadcast.</summary>
public sealed record ItemSelectedEvent(Guid SessionId, Guid ItemId, string Label) : IDomainEvent;
