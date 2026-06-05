# Scaffold Application Project

**Conditional step.** Only run this workflow if the CLAUDE.md shared-state axis is **Y** (service has cross-feature state: DTOs shared across features, state machines, seeders, mapping profiles, or a Features/ layer driven by MediatR).

If shared-state = N, skip this workflow entirely. The service has no `.Application` project, and `.API` references `.Persistence` directly.

## Reference exemplars

- **MediatR services (Carter / Minimal APIs):** `src/Services/{Svc}/{Svc}.Application/` — full Features/{Aggregate}/{Feature} layout
- **FastEndpoints with shared state (no Features):** `src/Services/{Svc}/{Svc}.Application/` — StateMachines/, Dtos/, Mappings/ only, no Features/

## `{Name}.Application/GlobalUsings.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Application/GlobalUsings.cs`

```csharp
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Blocks.Core;
global using {Name}.Domain;
global using {Name}.Persistence;
```

Add MediatR usings (`global using MediatR;`) only for MediatR framework branches.

Add integration event usings (`global using {ProjectName}.Integration.Contracts;`, `global using MassTransit;`) only if events are published or consumed from this layer.

## `{Name}.Application/DependencyInjection.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Application/DependencyInjection.cs`

```csharp
namespace {Name}.Application;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Application(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR (only for Carter + MediatR or Minimal APIs + MediatR):
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyInjection>());

        // Mappings (AutoMapper, Mapperly, or hand-written — check exemplar):
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // State machines — registered by create-aggregate later

        return services;
    }
}
```

## Folder branches

### MediatR framework branch (Carter + MediatR, Minimal APIs + MediatR)

Keep all four folders:
- `{Name}.Application/Features/` — `create-feature` adds feature folders here
- `{Name}.Application/Dtos/`
- `{Name}.Application/StateMachines/`
- `{Name}.Application/Mappings/`

### FastEndpoints framework branch (shared state only)

Omit `Features/` — FastEndpoints collapses the feature slice into the endpoint class in `.API/Features/`. Keep:
- `{Name}.Application/Dtos/`
- `{Name}.Application/StateMachines/`
- `{Name}.Application/Mappings/`

## What goes in `.Application` vs `.API/Features`

**In `.Application` only:**
- Cross-feature reusable DTOs
- State machines (Stateless library)
- AutoMapper / Mapperly profiles
- Shared enums or constants used by multiple features
- Seeders that depend on application services

**In `.API/Features/{Aggregate}/{Feature}`:**
- Commands and command handlers (MediatR branch) OR endpoint classes (FastEndpoints branch)
- Feature-local validators
- Feature-local DTOs that aren't reused

## After this step

The `.Application` project compiles as an empty shell with just DI wire-up. Next: `ScaffoldInfrastructure.md`.
