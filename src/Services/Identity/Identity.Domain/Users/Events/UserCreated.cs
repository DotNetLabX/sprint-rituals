using Blocks.Domain.Events;

namespace Identity.Domain.Users.Events;

/// <summary>
/// Raised after a new User is persisted (after SaveChangesAsync, when EF has assigned the real PK).
/// Carries only the UserId — handlers re-load the user and its person as needed.
/// Drives the company-domain auto-seed announcement (first-administrator bootstrap only).
/// </summary>
public sealed record UserCreated(int UserId) : IDomainEvent;
