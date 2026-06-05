# Scaffold Domain Module — Core Projects

Scaffolds the `.Domain` + `.Persistence` projects of a Domain archetype module. Always runs for Domain modules. `.Application` and `.API` are conditional follow-ups.

**Reference exemplar:** `src/Modules/{Module}/{Module}.Domain/` + `src/Modules/{Module}/{Module}.Persistence/`

## Folder tree created

```
src/Modules/{Name}/
├── {Name}.Domain/
│   ├── Entities/            (empty — .gitkeep)
│   ├── Events/              (empty — .gitkeep)
│   ├── Enums/               (empty — .gitkeep)
│   ├── GlobalUsings.cs
│   └── {Name}.Domain.csproj
└── {Name}.Persistence/
    ├── EntityConfigurations/    (empty; EF branch only)
    ├── Repositories/            (empty)
    ├── Migrations/              (empty; EF branch only)
    ├── MasterData/              (empty)
    ├── {Name}DbContext.cs       (EF) or connection stub (Redis)
    ├── DependencyInjection.cs
    └── {Name}.Persistence.csproj
```

## `{Name}.Domain.csproj`

**Reference:** `src/Modules/{Module}/{Module}.Domain/{Module}.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Domain\Blocks.Domain.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\{ProjectName}.Abstractions\{ProjectName}.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

## `{Name}.Domain/GlobalUsings.cs`

```csharp
global using Blocks.Domain;
global using Blocks.Domain.Entities;
global using Blocks.Domain.ValueObjects;
global using {ProjectName}.Abstractions;
```

## `{Name}.Persistence.csproj`

**Reference:** `src/Modules/{Module}/{Module}.Persistence/{Module}.Persistence.csproj`

### EF Core branch (SQL Server or PostgreSQL)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.EntityFrameworkCore\Blocks.EntityFrameworkCore.csproj" />
    <ProjectReference Include="..\{Name}.Domain\{Name}.Domain.csproj" />
  </ItemGroup>
</Project>
```

### Redis branch

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Redis\Blocks.Redis.csproj" />
    <ProjectReference Include="..\{Name}.Domain\{Name}.Domain.csproj" />
  </ItemGroup>
</Project>
```

## `{Name}.Persistence/{Name}DbContext.cs` (EF branch only)

**Reference:** `src/Modules/{Module}/{Module}.Persistence/{Module}DbContext.cs`

### Embedded-host-connection branch

```csharp
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Blocks.EntityFrameworkCore;

namespace {Name}.Persistence;

public class {Name}DbContext : ApplicationDbContext
{
    public {Name}DbContext(DbContextOptions<{Name}DbContext> options) : base(options) { }

    // DbSet<Aggregate> properties added by create-aggregate

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("{SchemaName}");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Name}DbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

`HasDefaultSchema("{SchemaName}")` is **mandatory** in the embedded branch. The module's tables share the host's physical database and the schema gives them their own namespace. The module's migration history table also lives in this schema.

### Own-connection branch

Same as above but omit the `HasDefaultSchema(...)` line. The module has its own database, so schema isolation is unnecessary.

## `{Name}.Persistence/DependencyInjection.cs`

**Reference:** `src/Modules/{Module}/{Module}.Persistence/DependencyInjection.cs`

### Embedded-host-connection branch

```csharp
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {Name}.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Persistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<{Name}DbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<DbConnection>();
            options.UseSqlServer(connection, b => b.MigrationsHistoryTable("__EFMigrationsHistory", "{SchemaName}"));
            // PostgreSQL branch: options.UseNpgsql(connection, b => b.MigrationsHistoryTable("__EFMigrationsHistory", "{SchemaName}"))
        });

        // Repository registrations added by create-aggregate

        return services;
    }
}
```

The host **must** register a `DbConnection` in its DI container. This is a documented contract — the module does not try to create one.

### Own-connection branch

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {Name}.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Persistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<{Name}DbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("{Name}")));
        // PostgreSQL branch: options.UseNpgsql(configuration.GetConnectionString("{Name}"))

        return services;
    }
}
```

## Redis branch — Persistence DI

```csharp
using Redis.OM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {Name}.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Persistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("{Name}");
        services.AddSingleton(new RedisConnectionProvider(connectionString));
        // Repository<T> registrations added by create-aggregate
        return services;
    }
}
```

Redis modules always use their own connection string — there is no embedded-connection variant for Redis.

## After this step

The `.Domain` and `.Persistence` projects compile as empty shells. Next:
- If `Application layer: yes` → `ScaffoldDomainApplication.md`
- If `API layer: yes` → `ScaffoldDomainApi.md`
- Otherwise → `ScaffoldInfrastructure.md`
