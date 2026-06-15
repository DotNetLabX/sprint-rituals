namespace Reflekt.Domain.RetroSessions.Events;

public sealed record PhaseAdvancedEvent(RetroSession RetroSession) : IDomainEvent;
