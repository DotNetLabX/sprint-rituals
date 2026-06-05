---
name: add-pipeline-behavior
description: Creates a custom MediatR pipeline behavior for cross-cutting concerns. Use when adding validation, logging, caching, or other pipeline logic that applies to multiple handlers.
---

# Add Pipeline Behavior

Creates a custom MediatR `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns.

## Pattern

**Reference:** `src/BuildingBlocks/Blocks.MediatR/Behaviors/ValidationBehavior.cs`

### Create Behavior: `Blocks.MediatR/Behaviors/{Name}Behavior.cs`

```csharp
public class {Name}Behavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public {Name}Behavior({Dependencies}) { }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Before handler logic
        // ...

        var response = await next(); // call the actual handler

        // After handler logic
        // ...

        return response;
    }
}
```

### Constrained Behavior (Only Applies to Certain Requests)

```csharp
public class AssignUserIdBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableAction  // constraint — only runs for commands implementing IAuditableAction
{
}
```

## Registration

In `{Service}.Application/DependencyInjection.cs`, inside the `AddMediatR` lambda:

```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    config.AddOpenBehavior(typeof({Name}Behavior<,>));
});
```

**Order matters!** Behaviors execute in registration order:
1. `AssignUserIdBehavior` — sets user ID from JWT
2. `ValidationBehavior` — runs validators, throws on failure
3. `LoggingBehavior` — logs request/response timing
4. Your new behavior — add in the correct position

## Existing Behaviors

| Behavior | Constraint | Purpose |
|----------|-----------|---------|
| `AssignUserIdBehavior` | `TRequest : IAuditableAction` | Sets `CreatedById` from JWT claims |
| `ValidationBehavior` | `TRequest : IRequest<TResponse>` | Parallel validation, aggregated errors |
| `LoggingBehavior` | `TRequest : notnull, IRequest<TResponse>` + `TResponse : notnull` | Request/response timing |

## Location

Building block: `src/BuildingBlocks/Blocks.MediatR/Behaviors/{Name}Behavior.cs`

If service-specific: `{Service}.Application/Behaviors/{Name}Behavior.cs`
