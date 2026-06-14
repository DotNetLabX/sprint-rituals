
namespace Reflekt.Domain.Cards.Events;

public sealed record ReviewStatusChangedEvent(Card Card) : IDomainEvent;
