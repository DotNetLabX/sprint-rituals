# Scaffold Domain Module API Project

**Conditional step.** Only run if CLAUDE.md says `API layer: yes`.

A Domain module's `.API` project is a **class library** (not `Microsoft.NET.Sdk.Web`). The host service provides the web host; the module only contributes endpoints and feature services. The host mounts the module by calling `app.Map{Name}Endpoints()`.

## Framework compatibility — re-read the warning

A module with an API binds its framework choice to every host that mounts it. **Minimal APIs with an explicit `Map{Name}Endpoints()` extension is the only framework-agnostic option.** FastEndpoints and Carter only work when the host uses the same framework.

If the CLAUDE.md recorded `Endpoint framework: Minimal APIs` you are safe. Otherwise, the module is locked to single-framework hosts.

## Folder tree

```
src/Modules/{Name}/
└── {Name}.API/
    ├── Features/            (empty — .gitkeep)
    ├── GlobalUsings.cs
    ├── DependencyInjection.cs
    ├── EndpointRegistration.cs
    └── {Name}.API.csproj
```

No `Program.cs`, no `appsettings.json`, no `launchSettings.json` — this is a library, not a host.

## `{Name}.API.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.AspNetCore\Blocks.AspNetCore.csproj" />
    <!-- Application if present, otherwise Persistence directly: -->
    <ProjectReference Include="..\{Name}.Application\{Name}.Application.csproj" />
    <!-- or -->
    <ProjectReference Include="..\{Name}.Persistence\{Name}.Persistence.csproj" />
    <!-- Framework branch references — see below -->
  </ItemGroup>
</Project>
```

`<FrameworkReference Include="Microsoft.AspNetCore.App" />` is critical — it gives the class library access to `WebApplication`, `IEndpointRouteBuilder`, and other AspNetCore types without making it a web SDK project. This is the standard way to ship ASP.NET libraries.

### Framework-specific package references

**Minimal APIs** — no extra framework package. Just `Blocks.AspNetCore`.

**FastEndpoints:**
```xml
<ProjectReference Include="..\..\..\BuildingBlocks\Blocks.FastEndpoints\Blocks.FastEndpoints.csproj" />
```

**Carter + MediatR:**
```xml
<PackageReference Include="Carter" />
<ProjectReference Include="..\..\..\BuildingBlocks\Blocks.MediatR\Blocks.MediatR.csproj" />
```

## `{Name}.API/GlobalUsings.cs`

```csharp
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.DependencyInjection;
global using {Name}.Domain;
global using {Name}.Persistence;
// global using {Name}.Application;  (if present)
```

## `{Name}.API/DependencyInjection.cs`

```csharp
namespace {Name}.API;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Api(this IServiceCollection services)
    {
        // Feature services — handlers, validators, etc.
        // MediatR is already registered by the host; the module's assembly is scanned via Add{Name}Application
        return services;
    }
}
```

## `{Name}.API/EndpointRegistration.cs` — framework branch

### Minimal APIs (default, framework-agnostic)

```csharp
namespace {Name}.API;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder Map{Name}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/{name-lowercased}").WithTags("{Name}");

        // Feature endpoints register themselves here:
        // group.Map{FirstFeature}();

        return app;
    }
}
```

Each feature in `{Name}.API/Features/` defines a static `Map{FeatureName}()` extension that the module calls from `Map{Name}Endpoints()`.

### FastEndpoints

```csharp
namespace {Name}.API;

public static class EndpointRegistration
{
    // FastEndpoints discovers endpoints via assembly scanning.
    // The host must include this assembly when calling app.UseFastEndpoints().
    //
    // Host wiring example:
    //   app.UseFastEndpoints(c =>
    //   {
    //       c.Endpoints.Assemblies = [
    //           typeof({Name}.API.EndpointRegistration).Assembly,
    //           // ... host's own assembly
    //       ];
    //   });
    //
    // There is no Map{Name}Endpoints() method — FastEndpoints uses scanning instead.
}
```

Leave this file as a documentation stub. FastEndpoints cannot be mounted via an explicit extension method.

### Carter + MediatR

```csharp
namespace {Name}.API;

public static class EndpointRegistration
{
    // Carter discovers ICarterModule implementations via assembly scanning.
    // The host must include this assembly when calling app.AddCarter().
    //
    // Host wiring example:
    //   services.AddCarter(configurator: c =>
    //   {
    //       c.WithAssembly(typeof({Name}.API.EndpointRegistration).Assembly);
    //   });
    //
    // There is no Map{Name}Endpoints() method — Carter uses scanning instead.
}
```

Same note as FastEndpoints — Carter uses scanning, not explicit mounting.

## After this step

The `.API` project compiles as an empty class library with DI wire-up and an endpoint registration stub. Next: `ScaffoldInfrastructure.md`.
