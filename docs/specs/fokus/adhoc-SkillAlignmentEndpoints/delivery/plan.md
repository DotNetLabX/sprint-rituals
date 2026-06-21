# Skill Alignment Phase 2 — Endpoints, Validation Extensions, Seed Infrastructure

**Feature Spec:** None

## Context

SkillAlignment Phase 1 built the BuildingBlocks and refactored entities/persistence/domain event infrastructure. But the existing endpoints were not restructured to match the `create-feature` skill, the FluentValidation extensions referenced by the `create-feature` validator workflow don't exist, and the `SeedFromJsonFile` extension from `persistence-patterns` was deferred. This phase completes the alignment.

## Scope

**In scope:**
- Create FluentValidation extensions and MaxLength constants in Blocks.Core
- Create SeedFromJsonFile extension in Blocks.EntityFrameworkCore
- Restructure all endpoints to match the `create-feature` skill file structure (co-locate request+validator)
- Update validators to use skill-defined extensions
- Add Tags to endpoint Configure()

**Out of scope:**
- New features or endpoints
- Frontend changes
- Renaming endpoints (the endpoint class names are already correct)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | create-feature (Validator workflow) | Build | FluentValidation extensions + MaxLength constants in Blocks.Core | |
| 2 | persistence-patterns | Build | SeedFromJsonFile extension in Blocks.EntityFrameworkCore | |
| 3 | create-feature (EndpointFastEndpoints) | Follow | Restructure 6 endpoints to skill file structure | |
| 4 | create-feature (Validator workflow) | Follow | Update validators to use NotEmptyWithMessage, MaximumLengthWithMessage, MaxLength | |
| 5 | (none) | — | dotnet build + verify | |

## Implementation Steps

### Step 1: Create FluentValidation Extensions + MaxLength Constants

Build `create-feature` skill infrastructure (Validator workflow references these).

**Create:**
- `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs` — extension methods on `IRuleBuilder<T, TProperty>`:
  - `NotEmptyWithMessage(propertyName)` — consistent "'PropertyName' must not be empty."
  - `MaximumLengthWithMessage(maxLength, propertyName)` — consistent "'PropertyName' must not exceed {max} characters."
- `src/BuildingBlocks/Blocks.Core/MaxLength.cs` — static class with constants: `C0 = 0`, `C8 = 8`, `C16 = 16`, `C32 = 32`, `C64 = 64`, `C128 = 128`, `C256 = 256`, `C512 = 512`, `C1024 = 1024`, `C2048 = 2048`

Follow `create-feature` skill § `workflows/Validator.md` — Custom Validation Extensions and MaxLength Constants sections.

Blocks.Core.csproj needs a package reference to FluentValidation (for `IRuleBuilder`).

### Step 2: Create SeedFromJsonFile Extension

Build `persistence-patterns` skill infrastructure (EntityConfiguration base references this).

**Create:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs` — `SeedFromJsonFile<T>()` extension on `EntityTypeBuilder<T>`:
  - Reads `Data/Master/{typeof(T).Name}.json` from the output directory
  - Deserializes as `List<T>`
  - Calls `builder.HasData(items)`
  - No-ops gracefully if the JSON file doesn't exist (no crash on missing seed data)

Follow `persistence-patterns` skill § Seed Data — Master Data section.

**Modify:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs` — add `builder.SeedFromJsonFile()` call in base `Configure()` method, after HasKey setup.

### Step 3: Restructure Endpoints to Skill File Structure

Follow `create-feature` skill § Feature Folder Structure (FastEndpoints variant).

The skill expects 2 files per feature:
```
{FeatureName}Endpoint.cs    — endpoint + handler
{FeatureName}Command.cs     — request + response + validator (co-located)
```

For GET endpoints without request params, use `EndpointWithoutRequest<TResponse>` with response in the endpoint file.

**Restructure each endpoint:**

**GetSettings** (GET, no request):
- `GetSettingsEndpoint.cs` — keep as-is, move `GetSettingsResponse` into this file
- Delete `GetSettingsResponse.cs`
- Add `Tags("Settings")` in Configure()

**SaveSettings** (POST, has request+validator):
- `SaveSettingsEndpoint.cs` — keep endpoint logic, add `Tags("Settings")` in Configure()
- Merge `SaveSettingsRequest.cs` + `SaveSettingsResponse.cs` + `SaveSettingsValidator.cs` → `SaveSettingsCommand.cs` (request + response + validator co-located)
- Delete the 3 separate files

**GetBoards** (GET, no request):
- `GetBoardsEndpoint.cs` — keep as-is, move `GetBoardsResponse` into this file
- Delete `GetBoardsResponse.cs`
- Add `Tags("Sync")` in Configure()

**GetJiraSprints** (GET, has request+validator):
- `GetJiraSprintsEndpoint.cs` — keep endpoint logic, add `Tags("Sync")` in Configure()
- Merge `GetJiraSprintsRequest.cs` + `GetJiraSprintsResponse.cs` + `GetJiraSprintsValidator.cs` → `GetJiraSprintsQuery.cs` (request + response + validator co-located)
- Delete the 3 separate files

**SyncSprints** (POST, has request+validator):
- `SyncSprintsEndpoint.cs` — keep endpoint logic, add `Tags("Sync")` in Configure()
- Merge `SyncSprintsRequest.cs` + `SyncSprintsResponse.cs` + `SyncSprintsValidator.cs` → `SyncSprintsCommand.cs` (request + response + validator co-located)
- Delete the 3 separate files

**SyncBacklog** (POST, no request):
- `SyncBacklogEndpoint.cs` — keep as-is, move `SyncBacklogResponse` into this file
- Delete `SyncBacklogResponse.cs`
- Add `Tags("Sync")` in Configure()

**Naming convention:** Use `Command` for POST (mutates state), `Query` for GET with request params. Endpoints without request params put the response in the endpoint file.

### Step 4: Update Validators to Use Skill Extensions

Follow `create-feature` skill § `workflows/Validator.md`.

**Modify validators** (now co-located in Command/Query files):
- `SaveSettingsCommand.cs` validator — use `MaximumLengthWithMessage` where string lengths are validated
- `GetJiraSprintsQuery.cs` validator — already simple (GreaterThan), no string validation to convert
- `SyncSprintsCommand.cs` validator — already simple (GreaterThan), no string validation to convert

Only convert where applicable — don't force the extensions where plain FluentValidation is clearer (e.g., `GreaterThan(0)` has no skill-defined extension).

### Step 5: Verify Build

1. Run `dotnet build src/Fokus.slnx` — must compile with zero errors
2. Confirm all endpoints still resolve (no broken namespaces from file moves)

## Testing Strategy

- Build passes with zero errors
- All endpoint classes compile and their validators are auto-discovered (same namespace, co-located)
- SeedFromJsonFile extension compiles and no-ops when no JSON files exist

## Open Questions

None.
