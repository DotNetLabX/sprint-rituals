
namespace Reflekt.Domain.Sprints.Events;

public sealed record SprintClosedEvent(Sprint Sprint) : IDomainEvent;
