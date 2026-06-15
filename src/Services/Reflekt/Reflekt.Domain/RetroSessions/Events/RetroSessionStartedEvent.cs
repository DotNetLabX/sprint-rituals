namespace Reflekt.Domain.RetroSessions.Events;

public sealed record RetroSessionStartedEvent(RetroSession RetroSession) : IDomainEvent;
