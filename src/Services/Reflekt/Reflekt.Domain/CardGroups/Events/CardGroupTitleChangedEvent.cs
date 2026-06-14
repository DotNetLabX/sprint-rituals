namespace Reflekt.Domain.CardGroups.Events;

public sealed record CardGroupTitleChangedEvent(CardGroup Group) : IDomainEvent;
