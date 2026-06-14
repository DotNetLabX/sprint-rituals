
namespace Reflekt.Domain.Cards.Events;

public sealed record CardCreatedEvent(Card Card) : IDomainEvent;
