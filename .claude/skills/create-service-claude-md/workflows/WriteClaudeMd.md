# Write Service CLAUDE.md

After every axis is filled and the user has confirmed the summary, write the file at:

```
src/Services/{Name}/CLAUDE.md
```

The folder `src/Services/{Name}/` does not yet exist — create it as part of writing the file.

## Output format

Match the shape of existing service CLAUDE.md files. Check `src/Services/` for exemplars covering different framework + persistence combinations (e.g., MediatR + Carter, FastEndpoints + SQL Server, FastEndpoints + Redis, Carter read-model + PostgreSQL).

## Template

```markdown
# {Name} Service

**Endpoint framework:** {framework}
**Database:** {persistence details — e.g., "SQL Server ({Name}Db)", "PostgreSQL ({Name}Db)", "Redis ({Name}.OM)", "SQL Server ({Name}Db) + MongoGridFS (files)"}
**Port:** {http} / {https}

## Purpose

{one paragraph — the purpose captured in axis 2}

## Domain model

- **Aggregate:** TBD — populated after `create-aggregate` runs
- **Key entities:** TBD
- **Application layer:** {"Separate project (`{Name}.Application`)" if Y, otherwise "None — handler logic in endpoints"}

## Endpoint pattern

{paragraph describing the chosen framework's pattern. Use the appropriate canned text below based on framework axis}

### FastEndpoints (canned text)
> Class extending `Endpoint<TRequest, TResponse>`. Handler logic lives directly in `HandleAsync`. Attribute-based routing (`[HttpPost]`, `[HttpGet]`, `[Tags]`). Validators extend `Validator<T>` (or `BaseValidator<T>` if you add a custom base). Endpoints are auto-discovered by `AddFastEndpoints()`.

### Carter + MediatR (canned text)
> `ICarterModule` with `AddRoutes(IEndpointRouteBuilder)`. Endpoints dispatch to MediatR via `ISender.Send()`. Handlers live in `{Name}.Application/Features/{Aggregate}/{Feature}/`. Pipeline behaviors: `ValidationBehavior`, `LoggingBehavior`, `AssignUserIdBehavior`. Endpoints auto-discovered by `AddCarter()`.

### Minimal APIs + MediatR (canned text)
> Static class with `Map()` extension method, composed via `EndpointRegistration.MapAllEndpoints()`. Endpoints dispatch to MediatR via `ISender.Send()`. Handlers live in `{Name}.Application/Features/{Aggregate}/{Feature}/`. Pipeline behaviors: `ValidationBehavior`, `LoggingBehavior`, `AssignUserIdBehavior`. Endpoints registered manually in `Program.cs`.

### Carter read-model (canned text)
> `ICarterModule` with `AddRoutes(IEndpointRouteBuilder)`. No MediatR — queries and consumers live directly in the `.API` project. No domain behavior — pure read model populated by MassTransit integration event consumers.

## MediatR pipeline behaviors

{list if MediatR; "N/A" otherwise. Standard list: AssignUserIdBehavior, ValidationBehavior, LoggingBehavior}

## File storage

{only if secondary persistence is configured, e.g., "MongoGridFS (singleton registration)" or "Azure Blob"; otherwise omit this section}

## Existing features

None — new service. Populated as features are added.

## gRPC clients

{list each consumed client or "None"}

## gRPC server

{list each exposed service with methods, or "None"}

## Integration events

**Published:** {list or "None"}
**Consumed:** {list or "None"}
```

## Folder structure note (for reference only — skill does NOT scaffold folders)

The Developer's `create-service` skill will scaffold:
- `{Name}.API/` (always)
- `{Name}.Domain/` (always)
- `{Name}.Persistence/` (always)
- `{Name}.Application/` (only if shared-state axis = Y)

This Architect skill only writes the CLAUDE.md file.

## After writing

Report to the user:

> Service CLAUDE.md ready at `src/Services/{Name}/CLAUDE.md`.
> The Developer can now run `/create-service {Name}`.
