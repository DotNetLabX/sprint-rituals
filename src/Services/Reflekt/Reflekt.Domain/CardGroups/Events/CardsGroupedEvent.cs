namespace Reflekt.Domain.CardGroups.Events;

public sealed record CardsGroupedEvent(CardGroup Group, int CardId, int SourceGroupId) : IDomainEvent;
