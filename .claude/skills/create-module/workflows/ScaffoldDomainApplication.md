# Scaffold Domain Module Application Project

**Conditional step.** Only run if the CLAUDE.md says `Application layer: yes`.

A Domain module's `.Application` project holds cross-feature state, event handlers, DTOs, and mapping profiles. It depends on `.Persistence` (no repository interfaces in `.Domain` — same rule as services).

**Reference exemplar:** Follow the shape of an existing service's `.Application` project (e.g., `src/Services/{Svc}/{Svc}.Application/`) as the pattern, scoped down for a module.

## Folder tree

```
src/Modules/{Name}/
└── {Name}.Application/
    ├── EventHandlers/       (empty — .gitkeep)
    ├── Dtos/                (empty — .gitkeep)
    ├── Mappings/            (empty — .gitkeep)
    ├── GlobalUsings.cs
    ├── DependencyInjection.cs
    └── {Name}.Application.csproj
```

## `{Name}.Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Core\Blocks.Core.csproj" />
    <!-- MediatR only if the API layer uses MediatR: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.MediatR\Blocks.MediatR.csproj" />
    <ProjectReference Include="..\{Name}.Persistence\{Name}.Persistence.csproj" />
  </ItemGroup>
</Project>
```

Comment out the `Blocks.MediatR` reference if the module's API framework is FastEndpoints (FastEndpoints uses its own event dispatch, not MediatR).

## `{Name}.Application/GlobalUsings.cs`

```csharp
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Blocks.Core;
global using {Name}.Domain;
global using {Name}.Persistence;
```

Add `global using MediatR;` only for MediatR framework branches.

## `{Name}.Application/DependencyInjection.cs`

```csharp
namespace {Name}.Application;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Application(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR (if using MediatR framework):
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyInjection>());

        // Mapping profiles — register here

        return services;
    }
}
```

## What lives here vs `.API/Features`

**In `.Application`:**
- Cross-feature DTOs (reused by more than one feature)
- Domain event handlers that need application-level services
- Mapping profiles (AutoMapper, Mapperly)
- State machines

**In `.API/Features`:**
- Feature-local commands/queries, handlers, endpoints, validators, DTOs

## After this step

The `.Application` project compiles as an empty shell. Next:
- If `API layer: yes` → `ScaffoldDomainApi.md`
- Otherwise → `ScaffoldInfrastructure.md`
