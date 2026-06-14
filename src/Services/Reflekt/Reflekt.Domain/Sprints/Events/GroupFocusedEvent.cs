
namespace Reflekt.Domain.Sprints.Events;

public sealed record GroupFocusedEvent(Sprint Sprint, int GroupId, int? PreviousGroupId) : IDomainEvent;
