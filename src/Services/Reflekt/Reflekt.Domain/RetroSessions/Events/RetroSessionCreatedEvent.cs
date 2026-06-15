namespace Reflekt.Domain.RetroSessions.Events;

public sealed record RetroSessionCreatedEvent(RetroSession RetroSession) : IDomainEvent;
