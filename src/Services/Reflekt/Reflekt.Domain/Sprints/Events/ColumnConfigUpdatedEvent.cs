
namespace Reflekt.Domain.Sprints.Events;

public sealed record ColumnConfigUpdatedEvent(Sprint Sprint) : IDomainEvent;
