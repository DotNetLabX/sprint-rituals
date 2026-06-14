namespace Reflekt.Domain.CardGroups.Events;

public sealed record CardUngroupedEvent(CardGroup SourceGroup, int CardId, int NewGroupId) : IDomainEvent;
