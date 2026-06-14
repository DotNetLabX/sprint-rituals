
namespace Reflekt.Domain.Sprints.Events;

public sealed record PhaseAdvancedEvent(Sprint Sprint) : IDomainEvent;
