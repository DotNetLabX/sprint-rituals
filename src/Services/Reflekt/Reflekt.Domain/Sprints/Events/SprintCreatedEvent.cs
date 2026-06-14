
namespace Reflekt.Domain.Sprints.Events;

public sealed record SprintCreatedEvent(Sprint Sprint) : IDomainEvent;
