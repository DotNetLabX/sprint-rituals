# Scaffold Domain Project

Create the `.Domain` project source files. The domain project is intentionally thin at scaffold time — only `GlobalUsings.cs` and empty folders. Aggregates, value objects, and domain events are added by the Developer later via `create-aggregate`.

## `{Name}.Domain/GlobalUsings.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Domain/GlobalUsings.cs`

```csharp
global using Blocks.Domain;
global using Blocks.Domain.Entities;
global using Blocks.Domain.ValueObjects;
global using {ProjectName}.Abstractions;
```

Add more usings only if a specific aggregate needs them later.

## Empty folders

These were already created by `ScaffoldFolders.md`. Confirm each has a `.gitkeep` if your file system does not track empty directories:

- `{Name}.Domain/Entities/` — aggregates and entities
- `{Name}.Domain/Events/` — domain events
- `{Name}.Domain/ValueObjects/` — value objects and enums

## No other files

Do not create:
- A sample aggregate — `create-aggregate` handles this.
- A base class — use `Blocks.Domain` types.
- `DependencyInjection.cs` — the domain project has no DI registrations.

## After this step

The `.Domain` project compiles as an empty shell. Next: `ScaffoldPersistenceProject.md`.
