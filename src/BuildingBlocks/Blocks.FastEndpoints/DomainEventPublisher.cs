using Blocks.Domain.Events;
using FastEndpoints;

namespace Blocks.FastEndpoints;

public class DomainEventPublisher : IDomainEventPublisher
{
    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await domainEvent.PublishAsync(Mode.WaitForAll, ct);
        }
    }
}
