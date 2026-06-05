using Blocks.Domain.Events;

namespace Identity.Domain.Persons.Events;

public sealed record PersonCreated(int PersonId) : IDomainEvent;
