
namespace Reflekt.Domain.Sprints.Events;

public sealed record GroupFocusClearedEvent(Sprint Sprint, int? PreviousGroupId) : IDomainEvent;
