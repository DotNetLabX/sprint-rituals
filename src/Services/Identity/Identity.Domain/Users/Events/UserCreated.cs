using Blocks.Domain.Events;

namespace Identity.Domain.Users.Events;

/// <summary>
/// Intentional deferred-infra placeholder declared ahead of its consumer.
/// Wiring is deferred to the Identity infra increment (invite redemption,
/// CompanyDomain seed, PersonGrpcService). Not raised anywhere today — <c>User</c>
/// is an <see cref="Blocks.Domain.Entities.Entity{TKey}"/>, not an AggregateRoot,
/// so domain-event dispatch is not yet available on it. Do not delete as dead code;
/// do not add a RaiseCreated call without first promoting User to AggregateRoot.
/// </summary>
public sealed record UserCreated(int UserId) : IDomainEvent;
