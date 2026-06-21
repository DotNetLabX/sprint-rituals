# Skill Alignment Phase 2 — Implementation

## Files Created

- `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs` — `NotEmptyWithMessage` and `MaximumLengthWithMessage` extension methods on `IRuleBuilder<T, TProperty>`
- `src/BuildingBlocks/Blocks.Core/MaxLength.cs` — static class with C0–C2048 constants
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs` — `SeedFromJsonFile<T>()` extension on `EntityTypeBuilder<T>`; reads `Data/Master/{TypeName}.json` from output dir, no-ops if file missing
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — merged `SaveSettingsRequest` + `SaveSettingsResponse` + `SaveSettingsValidator` into one file
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsQuery.cs` — merged `GetJiraSprintsRequest` + `GetJiraSprintsResponse` + `JiraSprintDto` + `GetJiraSprintsValidator` into one file
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs` — merged `SyncSprintsRequest` + `SyncSprintsResponse` + `SprintSyncFailure` + `SyncSprintsValidator` into one file

## Files Modified

- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj` — added `FluentValidation 11.11.0` package reference (required for `IRuleBuilder` in Extensions.cs)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs` — added `builder.SeedFromJsonFile()` call in both `Configure()` overloads (int key and generic key variants), after the HasKey/ValueGenerated setup; added `using Blocks.EntityFrameworkCore.Extensions`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — moved `GetSettingsResponse` class into this file; added `[Tags("Settings")]` attribute; added `using Fokus.Domain.ValueObjects`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — added `[Tags("Settings")]` attribute; removed now-redundant using statements (request/response/validator now in Command file)
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs` — moved `GetBoardsResponse` and `BoardDto` classes into this file; added `[Tags("Sync")]` attribute
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs` — added `[Tags("Sync")]` attribute; removed using statements for deleted separate files
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — added `[Tags("Sync")]` attribute; removed using statements for deleted separate files
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — moved `SyncBacklogResponse` class into this file; added `[Tags("Sync")]` attribute

## Files Deleted

- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsRequest.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsValidator.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsRequest.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsValidator.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsRequest.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsValidator.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogResponse.cs`

## Key Decisions

- FluentValidation 11.11.0 added to Blocks.Core to match the transitive version already brought in by FastEndpoints 6.0.0, avoiding a version conflict.
- `using FluentValidation;` was required in the merged Command/Query files even though `Validator<T>` comes from FastEndpoints — the FluentValidation rule builder extension methods (`GreaterThan`, `NotEmpty`, `InclusiveBetween`, `Must`, etc.) live in the FluentValidation namespace. The original separate validator files had this using explicitly; it was omitted in the initial merged files, causing a build error that was fixed before the final build.
- Step 4 (update validators to use skill extensions) was a no-op: none of the three Command/Query validators contain string property length validation. `SaveSettingsRequest` has no plain string fields with length constraints. The plan explicitly states "only convert where applicable."
- `SeedFromJsonFile` uses `AppContext.BaseDirectory` (output directory) to locate JSON files, consistent with the persistence-patterns skill's convention.

## Deviations from Plan

None. All steps executed as specified.
