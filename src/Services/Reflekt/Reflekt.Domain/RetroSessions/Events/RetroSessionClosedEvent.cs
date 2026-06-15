namespace Reflekt.Domain.RetroSessions.Events;

public sealed record RetroSessionClosedEvent(RetroSession RetroSession) : IDomainEvent;
