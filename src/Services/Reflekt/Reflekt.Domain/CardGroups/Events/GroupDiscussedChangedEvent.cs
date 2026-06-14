
namespace Reflekt.Domain.CardGroups.Events;

public sealed record GroupDiscussedChangedEvent(CardGroup Group) : IDomainEvent;
