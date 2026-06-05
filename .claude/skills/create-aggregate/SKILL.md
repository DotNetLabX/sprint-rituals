---
name: create-aggregate
description: Creates a DDD aggregate root with behavior split, value objects, domain events, EF config, and repository. Variant-aware for EF Core and Redis persistence. Use when adding a new aggregate to a service's domain model.
---

# Create Aggregate

Creates a DDD aggregate root following the partial class behavior split pattern.

## Steps

1. **Create the aggregate** — variant determines the pattern:
   - EF Core service → `workflows/AggregateEfCore.md`
   - Redis service → `workflows/AggregateRedis.md`

2. **Create value objects** (if needed) — follow `workflows/ValueObject.md`

3. **Create domain events** (if needed) — follow `workflows/DomainEvent.md`

4. **Create EF Core entity configuration** (EF variant only) — included in `workflows/AggregateEfCore.md`

5. **Create/update the repository** — included in the variant workflow

6. **Add migration** (EF variant only):
   ```bash
   dotnet ef migrations add {Name} -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API
   ```

7. **Verify the build:** `dotnet build`

## Arguments

Pass the aggregate name: `/create-aggregate OrderItem`

## Special Cases

- **Identity `User`:** Cannot extend `AggregateRoot<T>` due to `IdentityUser` constraint — must implement `IAggregateRoot` manually
- **Redis services:** Uses `Blocks.Redis.Entity` base, not `AggregateRoot`. No audit fields, no domain event queue on the base class. Domain events still exist (`IDomainEvent` records) and are dispatched via FastEndpoints `IEventHandler`.
