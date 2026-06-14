namespace Reflekt.Domain.CardGroups.Events;

public sealed record CardGroupCreatedEvent(CardGroup Group) : IDomainEvent;
