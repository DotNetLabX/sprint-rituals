# Scaffold csproj Files

Create the `.csproj` files for each layer of the new service with the correct `ProjectReference` wiring. References follow the architecture rules in root `CLAUDE.md`:

- `.Application` depends on `.Persistence` **directly** — no repository interfaces in `.Domain`.
- `.Persistence` depends only on its own `.Domain` + the EF/Redis building block.
- `.Domain` depends only on `Blocks.Domain`.
- `.API` depends on `.Application` (if present) or `.Persistence` (if not).

## {Name}.Domain.csproj

**Reference:** `src/Services/{Svc}/{Svc}.Domain/{Svc}.Domain.csproj`

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

## {Name}.Persistence.csproj

**Reference (EF Core variants):** `src/Services/{Svc}/{Svc}.Persistence/{Svc}.Persistence.csproj`
**Reference (Redis variant):** `src/Services/{Svc}/{Svc}.Persistence/{Svc}.Persistence.csproj`

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

For PostgreSQL, the same Blocks.EntityFrameworkCore reference applies — the provider (`Npgsql.EntityFrameworkCore.PostgreSQL`) is added at the API project level via `options.UseNpgsql(...)` in the DI setup.

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

## {Name}.Application.csproj (conditional)

**Only created if shared-state axis = Y.**

**Reference:** `src/Services/{Svc}/{Svc}.Application/{Svc}.Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Core\Blocks.Core.csproj" />
    <!-- MediatR variants only: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.MediatR\Blocks.MediatR.csproj" />
    <!-- Integration events only: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Messaging\Blocks.Messaging.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\{ProjectName}.Integration.Contracts\{ProjectName}.Integration.Contracts.csproj" />
    <!-- gRPC clients only: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\{ProjectName}.Grpc.Contracts\{ProjectName}.Grpc.Contracts.csproj" />
    <!-- Always: -->
    <ProjectReference Include="..\{Name}.Persistence\{Name}.Persistence.csproj" />
  </ItemGroup>

</Project>
```

Comment out (or omit) the `<ProjectReference>` lines that do not apply to the new service's axes.

## {Name}.API.csproj

**Reference:** `src/Services/{Svc}/{Svc}.API/{Svc}.API.csproj` (check an existing service matching your endpoint framework)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>{new GUID}</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <!-- Docker axis = Y: -->
    <DockerComposeProjectPath>..\..\..\docker-compose.dcproj</DockerComposeProjectPath>
  </PropertyGroup>

  <ItemGroup>
    <!-- Framework branch: -->
    <!-- FastEndpoints: no endpoint framework package here, just Blocks.FastEndpoints reference below -->
    <!-- Carter: -->
    <PackageReference Include="Carter" />
    <!-- Minimal APIs: no extra framework package -->
    <!-- Common: -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <!-- EF Core tooling (only if EF Core persistence): -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Http.Polly" />
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.AspNetCore\Blocks.AspNetCore.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\{ProjectName}.Security\{ProjectName}.Security.csproj" />
    <!-- Framework branch: -->
    <!-- FastEndpoints: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.FastEndpoints\Blocks.FastEndpoints.csproj" />
    <!-- Carter + MediatR OR Minimal APIs + MediatR: -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.MediatR\Blocks.MediatR.csproj" />
    <!-- Integration events (if any published or consumed): -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Blocks.Messaging\Blocks.Messaging.csproj" />
    <!-- gRPC clients (if any consumed): -->
    <ProjectReference Include="..\..\..\BuildingBlocks\{ProjectName}.Grpc.Contracts\{ProjectName}.Grpc.Contracts.csproj" />
    <PackageReference Include="Grpc.Net.Client" />
    <!-- File storage (if configured): -->
    <!-- e.g., <ProjectReference Include="..\..\..\Modules\FileService\FileService.AzureBlob\FileService.AzureBlob.csproj" /> -->
    <!-- Application layer if present, otherwise Persistence directly: -->
    <ProjectReference Include="..\{Name}.Application\{Name}.Application.csproj" />
    <!-- or -->
    <ProjectReference Include="..\{Name}.Persistence\{Name}.Persistence.csproj" />
  </ItemGroup>

</Project>
```

Comment out lines that do not apply. Generate a fresh GUID for `<UserSecretsId>`.

## Rule: one reference path per concern

When both `.Application` and `.Persistence` exist, `.API` references **only** `.Application` — the Application project transitively pulls in Persistence. Do not double-reference.

When `.Application` is absent, `.API` references `.Persistence` directly.

## After this step

All csproj files exist with correct references. The solution does not yet compile because there are no source files. Next: `ScaffoldApiProject.md` / `ScaffoldDomainProject.md` / `ScaffoldPersistenceProject.md` / `ScaffoldApplicationProject.md` create the source files.
