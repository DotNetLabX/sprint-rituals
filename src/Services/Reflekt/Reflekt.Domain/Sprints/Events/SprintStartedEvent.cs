
namespace Reflekt.Domain.Sprints.Events;

public sealed record SprintStartedEvent(Sprint Sprint) : IDomainEvent;
