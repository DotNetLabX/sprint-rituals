
namespace Reflekt.Domain.Cards.Events;

public sealed record EvidenceLinkFetchedEvent(Card Card, EvidenceLink EvidenceLink) : IDomainEvent;
