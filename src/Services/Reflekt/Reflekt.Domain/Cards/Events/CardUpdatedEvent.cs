
namespace Reflekt.Domain.Cards.Events;

public sealed record CardUpdatedEvent(Card Card) : IDomainEvent;
