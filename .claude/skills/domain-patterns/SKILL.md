---
name: domain-patterns
description: DDD patterns — aggregates, entities, value objects, domain events, partial class split, state machines, event dispatch. Loaded when designing or implementing domain models.
user-invocable: true
---

# Domain Patterns

## AggregateRoot

**File:** `src/BuildingBlocks/Blocks.Domain/Entities/AggregateRoot.cs`

- Extends `Entity<TPrimaryKey>`, adds audit fields (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`)
- Private `_domainEvents` list exposed as `IReadOnlyList<IDomainEvent>`
- Methods: `AddDomainEvent()`, `ClearDomainEvents()`
- Convenience form: `AggregateRoot` (non-generic, PK = int)
- **Variant:** Identity `User` extends `IdentityUser<int>` and implements `IAggregateRoot` manually (ASP.NET Identity constraint)

## Entity

**File:** `src/BuildingBlocks/Blocks.Domain/Entities/Entity.cs`

- `Entity<TPrimaryKey>` with `Id` property and equality by ID
- `IsNew` returns true when ID is default value
- Transient entities (both `IsNew`) are never considered equal

## Value Objects

**File:** `src/BuildingBlocks/Blocks.Domain/ValueObjects/`

Three base types:
- `ValueObject` — multi-property, implements `GetEqualityComponents()`
- `StringValueObject` — single string `Value`, full equality (`Equals`, `==`/`!=`, `GetHashCode`)
- `SingleValueObject<T>` — single struct `Value`, handles boxed `T` in `Equals(object?)`

Pattern: static `Create()` factory with validation, internal constructor:
```csharp
public class EmailAddress : StringValueObject
{
    internal EmailAddress(string value) { Value = value; }
    public static EmailAddress Create(string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(value);
        return new EmailAddress(value);
    }
}
```

**Per-bounded-context duplication is intentional.** Each service defines its own VOs even if the shape is identical. This follows DDD — each BC owns its types. Only share VOs through BuildingBlocks when they are truly cross-cutting (e.g., `SingleValueObject<T>` base).

## Partial Class Behavior Split

All services split aggregate state from behavior:
- `{Aggregate}.cs` — properties, backing collections
- `Behaviors/{Aggregate}.cs` — domain methods that mutate state and raise events

Backing collection pattern:
```csharp
private readonly List<LineItem> _lineItems = new();
public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();
```

Behavior method convention — **action parameter is typically last.** Prefer action last when writing new code:
```csharp
// action last (preferred convention)
public void AssignTo(Assignee assignee, IAction action)
{
    // validate
    // mutate state
    AddDomainEvent(new AssigneeChanged(this, action));
}
```

## Domain Events

**Interface:** `IDomainEvent : INotification, IEvent` (dual MediatR + FastEndpoints)

Two first-class variants:

**Variant A — With action tracking:**
```csharp
public abstract record DomainEvent<TAction>(TAction Action) : IDomainEvent;
public record OrderApproved(Order Order, IAction Action) : DomainEvent<IAction>(Action);
```
Services that track which action triggered an event use this variant. `TAction` is a per-service action marker interface (e.g., `IOrderAction`).

**Variant B — Aggregate reference only:**
```csharp
public sealed record OrderCreated(Order Order) : IDomainEvent;
```
Services where events are simple notifications use this variant.

## Event Dispatch

Two interceptor variants in `Blocks.EntityFrameworkCore/Interceptors/`:

| Interceptor | When to use | Behavior |
|-------------|-------------|----------|
| `DispatchDomainEventsInterceptor` | Standard (default for most services) | Dispatches after SaveChanges completes, no transaction |
| `TransactionalDispatchDomainEventsInterceptor` | Transactional (when event handlers write to the same DB in the same request) | Wraps save + dispatch in single transaction, rolls back on failure |

Use **standard** by default. Use **transactional** when domain event handlers write to the same DB in the same request (e.g., aggregate + timeline sharing a DbContext).

Both scan `ChangeTracker` for aggregates with pending events, clear them, then publish via `IDomainEventPublisher`.

## IDomainEventPublisher — Framework Variants

| Implementation | Used by | Mechanism |
|---------------|---------|-----------|
| `Blocks.MediatR/DomainEventPublisher` | MediatR services | `IMediator.Publish()` → `INotificationHandler<T>` |
| `Blocks.FastEndpoints/DomainEventPublisher` | FastEndpoints-native services | `IEvent.PublishAsync(Mode.WaitForAll)` → `IEventHandler<T>` |

The variant follows the CQRS/dispatch axis. Some services mix endpoint frameworks — FastEndpoints for HTTP but MediatR for event dispatch (when `INotificationHandler` pipeline is needed). Check the service's CLAUDE.md for which variant to use.

## State Machine Pattern

**File:** `src/Services/{Svc}/{Svc}.Application/StateMachines/`

- `{Aggregate}StateMachineFactory` registered as delegate factory — avoids injecting into aggregates
- Validates stage transitions via transition table stored in DB (`{Aggregate}StageTransition`)
- Called in aggregate behavior methods before state mutation
- Per-service `{Aggregate}ActionType` enum defines allowed actions

The pattern is: DB-stored transition rules + factory-provided validator + aggregate-enforced invariants.
