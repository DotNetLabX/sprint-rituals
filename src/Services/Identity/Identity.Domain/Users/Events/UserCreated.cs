using Blocks.Domain.Events;

namespace Identity.Domain.Users.Events;

public sealed record UserCreated(int UserId) : IDomainEvent;
