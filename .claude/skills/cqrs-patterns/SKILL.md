---
name: cqrs-patterns
description: CQRS patterns — commands, queries, handlers, MediatR pipeline behaviors. Loaded when implementing features with MediatR dispatch.
user-invocable: true
---

# CQRS Patterns

## Command/Query Interfaces

**Files:**
- `src/BuildingBlocks/Blocks.MediatR/Abstractions/ICommand.cs`
- `src/BuildingBlocks/Blocks.MediatR/Abstractions/IQuery.cs`

```csharp
public interface ICommand : ICommand<Unit>;
public interface ICommand<out TResponse> : IRequest<TResponse>;
public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
```

Marker interfaces for pipeline behavior targeting. Commands mutate state, queries read.

## Handler Pattern (Load-Mutate-Save)

```csharp
public class {Feature}CommandHandler({AggregateRepository} _repository, {Dependencies})
    : IRequestHandler<{Feature}Command, {ResponseType}>
{
    public async Task<{ResponseType}> Handle({Feature}Command command, CancellationToken ct)
    {
        var entity = await _repository.FindByIdOrThrowAsync(command.{EntityId});

        entity.{DomainMethod}({params}, command, _stateMachineFactory);

        await _repository.SaveChangesAsync();

        return new {ResponseType}(entity.Id);
    }
}
```

Pattern: load aggregate → call domain method → save → return response. Domain events dispatch via interceptor on `SaveChangesAsync()`.

## Pipeline Behaviors

**File:** `src/BuildingBlocks/Blocks.MediatR/Behaviors/`

Registered in order inside `AddMediatR` lambda:
```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

### Pipeline Order
1. **AssignUserIdBehavior** — sets `CreatedById` on `IAuditableAction` commands from JWT claims
2. **ValidationBehavior** — runs all `IValidator<TRequest>` in parallel (`Task.WhenAll`), throws `ValidationException` with aggregated errors
3. **LoggingBehavior** — logs request/response timing

### AssignUserIdBehavior
Constrained to `IAuditableAction`. Calls `IClaimsProvider.TryGetUserId()`. This is the MediatR variant — other frameworks have equivalent mechanisms (see User ID Assignment variants below).

### ValidationBehavior
Discovers all `IValidator<TRequest>` via DI. Runs in parallel. Gracefully skips if no validators found.

```csharp
var failures = validationResults.Where(r => r.Errors.Any()).SelectMany(r => r.Errors).ToList();
if (failures.Any())
    throw new ValidationException(failures);
```

## User ID Assignment — Framework Variants

Same concept, different mechanism per framework:

| Variant | Framework | Mechanism | File |
|---------|-----------|-----------|------|
| MediatR Behavior | Carter, Minimal APIs | `IPipelineBehavior` on `IAuditableAction` | `Blocks.MediatR/Behaviors/AssignUserIdBehavior.cs` |
| FE PreProcessor | FastEndpoints | `IGlobalPreProcessor` | `Blocks.FastEndpoints/AssignUserIdPreProcessor.cs` |
| Endpoint Filter | Minimal APIs (direct) | `IEndpointFilter` on MapGroup | `Blocks.AspNetCore/Filters/AssignUserIdFilter.cs` |

MediatR Behavior and Endpoint Filter resolve `IClaimsProvider.TryGetUserId()`. FE PreProcessor uses `IClaimsProvider.GetUserId()` (non-nullable). All assign to `IAuditableAction.CreatedById`.

## Validator — Framework Variants

| Variant | Framework | Base Class | Discovery |
|---------|-----------|------------|-----------|
| FE Validator | FastEndpoints | `Validator<T>` | Auto-discovered by FastEndpoints |
| MediatR Validator | Carter, Minimal APIs | `AbstractValidator<T>` | `AddValidatorsFromAssemblyContaining<T>()` + `ValidationBehavior` |
| Custom base (opt-in per service) | FastEndpoints | `BaseValidator<T>` (extends `Validator<T>`) | Adds null-guard + logging |

Custom validation extensions in `Blocks.Core/FluentValidation/Extensions.cs`:
- `NotEmptyWithMessage(propertyName)`, `MaximumLengthWithMessage(maxLength, propertyName)` — consistent error message format

`MaxLength` constants in `Blocks.Core/MaxLength.cs`: `C0`, `C8`, `C16`, `C32`, `C64`, `C128`, `C256`, `C512`, `C1024`, `C2048`

## Registration

**File:** `src/Services/{Svc}/{Svc}.Application/DependencyInjection.cs`

```csharp
services
    .AddMapsterConfigsFromAssemblyContaining<{Domain}Mappings>()
    .AddValidatorsFromAssemblyContaining<{FeatureName}CommandValidator>()
    .AddMediatR(config =>
    {
        config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>));
        config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    });
```
