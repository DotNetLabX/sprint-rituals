
namespace Reflekt.Domain.Cards.Events;

public sealed record CardDeletedEvent(Card Card) : IDomainEvent;
