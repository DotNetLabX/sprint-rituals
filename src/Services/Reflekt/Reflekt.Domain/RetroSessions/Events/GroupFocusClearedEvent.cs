namespace Reflekt.Domain.RetroSessions.Events;

public sealed record GroupFocusClearedEvent(RetroSession RetroSession, int? PreviousGroupId) : IDomainEvent;
