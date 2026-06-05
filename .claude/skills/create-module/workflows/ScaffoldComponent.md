# Scaffold Component Module

Scaffolds a Component archetype module. Two modes depending on whether this is the first implementation or a later one.

**Reference exemplars:**
- `src/Modules/FileService/FileService.Contracts/` and `src/Modules/FileService/FileService.AzureBlob/`
- `src/Modules/EmailService/EmailService.Contracts/` and `src/Modules/EmailService/EmailService.Smtp/`

## Mode detection

```
if src/Modules/{Name}/{Name}.Contracts/ already exists:
    → Mode: second-run (impl only)
else:
    → Mode: first-run (contracts + first impl)
```

## Mode: first-run

### `{Name}.Contracts/{Name}.Contracts.csproj`

**Reference:** `src/Modules/FileService/FileService.Contracts/FileService.Contracts.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Shared dependencies:** If the contract interface returns or accepts types from other projects (e.g., BuildingBlocks packages, other modules' types), add `<ProjectReference>` entries for those dependencies. Check the CLAUDE.md for a `Shared dependencies` axis — if listed, add them to the Contracts csproj. If not listed but discovered during interface authoring, add them and update the CLAUDE.md.

### `{Name}.Contracts/I{ContractName}.cs`

**Reference:** `src/Modules/FileService/FileService.Contracts/IFileStorage.cs`

```csharp
namespace {Name}.Contracts;

public interface I{ContractName}
{
    // TODO: add contract methods
}
```

Leave the interface empty with a TODO. The Architect/Developer fills it based on the first feature that needs the module.

### Continue to the impl scaffold below

## Mode: second-run

Do not touch `{Name}.Contracts/`. Only scaffold the new implementation project.

Update `src/Modules/{Name}/CLAUDE.md`:
- Add the new implementation to the `**Implementations:**` line
- Add a `## {NewImpl} implementation` section near the existing `## Host wiring` section

## Implementation project (both modes)

### `{Name}.{ImplName}/{Name}.{ImplName}.csproj`

**Reference:** `src/Modules/FileService/FileService.AzureBlob/FileService.AzureBlob.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\{Name}.Contracts\{Name}.Contracts.csproj" />
    <!-- Add runtime packages as needed — e.g., Azure.Storage.Blobs, MailKit, etc. -->
  </ItemGroup>
</Project>
```

### `{Name}.{ImplName}/{ImplName}{Name}Options.cs` (if options class needed)

**Reference:** `src/Modules/FileService/FileService.AzureBlob/AzureBlobFileStorageOptions.cs`

```csharp
namespace {Name}.{ImplName};

public class {ImplName}{Name}Options
{
    public const string SectionName = "{ImplName}{Name}Options";

    // TODO: add configuration properties (connection strings, API keys, container names, etc.)
}
```

### `{Name}.{ImplName}/{ImplName}{ContractName}.cs`

**Reference:** `src/Modules/FileService/FileService.AzureBlob/AzureBlobFileStorage.cs`

```csharp
using {Name}.Contracts;

namespace {Name}.{ImplName};

public class {ImplName}{ContractName} : I{ContractName}
{
    // TODO: implement I{ContractName}
}
```

### `{Name}.{ImplName}/DependencyInjection.cs`

**Reference:** `src/Modules/FileService/FileService.AzureBlob/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {Name}.Contracts;

namespace {Name}.{ImplName};

public static class DependencyInjection
{
    public static IServiceCollection Add{ImplName}{Name}(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<{ImplName}{Name}Options>(
            configuration.GetSection({ImplName}{Name}Options.SectionName));

        services.AddSingleton<I{ContractName}, {ImplName}{ContractName}>();

        return services;
    }
}
```

## After this step

The Component module has `.Contracts` (first run only) and a fresh implementation project. Next: `ScaffoldInfrastructure.md` adds the new csprojs to the solution file.
