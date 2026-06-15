namespace Reflekt.Domain.RetroSessions.Events;

public sealed record ColumnConfigUpdatedEvent(RetroSession RetroSession) : IDomainEvent;
