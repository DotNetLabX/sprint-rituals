using Blocks.Domain.Events;

namespace Blocks.Domain.Entities;

public abstract class AggregateRoot<TPrimaryKey> : Entity<TPrimaryKey>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public string? CreatedById { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : AggregateRoot<int>;
