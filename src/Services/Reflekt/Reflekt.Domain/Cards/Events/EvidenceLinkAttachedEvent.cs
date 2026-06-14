
namespace Reflekt.Domain.Cards.Events;

public sealed record EvidenceLinkAttachedEvent(Card Card, EvidenceLink EvidenceLink) : IDomainEvent;
