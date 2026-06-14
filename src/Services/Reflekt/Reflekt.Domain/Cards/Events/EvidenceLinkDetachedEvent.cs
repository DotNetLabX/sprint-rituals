
namespace Reflekt.Domain.Cards.Events;

public sealed record EvidenceLinkDetachedEvent(Card Card, int EvidenceLinkId) : IDomainEvent;
