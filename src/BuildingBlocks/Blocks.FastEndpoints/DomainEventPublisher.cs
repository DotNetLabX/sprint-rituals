using Blocks.Domain.Events;
using FastEndpoints;

namespace Blocks.FastEndpoints;

public class DomainEventPublisher : IDomainEventPublisher
{
    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            // The generic PublishAsync<T> dispatches by compile-time T (IDomainEvent → no handlers);
            // the non-generic IEvent overload dispatches by runtime type.
            await ((IEvent)domainEvent).PublishAsync(Mode.WaitForAll, ct);
        }
    }
}
