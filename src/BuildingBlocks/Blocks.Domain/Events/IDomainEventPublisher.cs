namespace Blocks.Domain.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
