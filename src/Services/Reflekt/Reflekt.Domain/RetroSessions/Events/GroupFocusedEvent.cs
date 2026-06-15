namespace Reflekt.Domain.RetroSessions.Events;

public sealed record GroupFocusedEvent(RetroSession RetroSession, int GroupId, int? PreviousGroupId) : IDomainEvent;
