# ExtractJiraModule

**Feature Spec:** None

## Context

The Jira HTTP client infrastructure currently lives inside `Fokus.API/Infrastructure/Jira/` — a raw namespace folder in the host project. This couples the Jira integration to the API host and prevents reuse by other services or modules.

This refactoring extracts the Jira client infrastructure (typed client, Refit interface, options, DI wiring) into a proper Component module under `src/Modules/Jira/`, following the Contracts + Implementation split. `Fokus.JiraContracts` (the Jira API response contracts) stays as a BuildingBlock — it's shared between `Fokus.Domain` (which uses the contracts in `FromJira()` factory methods) and `Jira.Contracts` (which uses them as return types in `IJiraClient`). No behavior changes — same methods, same DI lifetime, same consumers.

## Scope

**In scope:**
- Create `src/Modules/Jira/Jira.Contracts/` with `IJiraClient` interface
- Create `src/Modules/Jira/Jira.RestApi/` with the concrete `RestApiJiraClient` (current `JiraClient`), `IJiraApi`, `JiraOptions`, and DI registration
- Update 4 consumer endpoints to depend on `IJiraClient` (from Contracts) instead of concrete `JiraClient`
- Update `Fokus.API.csproj` to reference `Jira.Contracts` + `Jira.RestApi`, remove `Refit.HttpClientFactory` package reference
- Remove `src/Services/Fokus/Fokus.API/Infrastructure/Jira/` folder
- Add both new module projects to `src/Fokus.slnx`
- Update `Fokus.API/DependencyInjection.cs` to call `services.AddRestApiJira(configuration)` instead of inline Refit+JiraClient registration
- Write `src/Modules/Jira/CLAUDE.md` (architect responsibility — the module's axis file)

**Out of scope:**
- Moving or absorbing `Fokus.JiraContracts` — stays as a BuildingBlock (shared between `Fokus.Domain` and `Jira.Contracts`)
- Moving `SprintState` enum — stays in `Fokus.Domain`
- Changing JiraClient behavior, method signatures, or error handling
- Adding new Jira API methods
- Rate limiting or retry policy changes
- Any domain model changes

## Dependency Graph

```
Jira.Contracts → Fokus.JiraContracts (for return types: JiraBoard, JiraSprint, JiraIssue, etc.)
Jira.Contracts → Fokus.Domain (for SprintState enum)
Jira.RestApi → Jira.Contracts
Jira.RestApi → Fokus.JiraContracts (for IJiraApi Refit interface return types)
Fokus.Domain → Fokus.JiraContracts (for FromJira() factory method parameters)
Fokus.API → Jira.Contracts + Jira.RestApi + Fokus.Domain + Fokus.Persistence + Fokus.JiraContracts
```

No cycles.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | create-module-claude-md | Follow | Name=Jira, Type=Component, Contract=IJiraClient, FirstImpl=RestApi | |
| 2 | create-module (ScaffoldComponent) | Follow | Name=Jira, Contract=IJiraClient, Impl=RestApi | |
| 3 | (none) | -- | Extract IJiraClient interface from JiraClient public methods | |
| 4 | (none) | -- | Move and rename implementation files | |
| 5 | (none) | -- | Wire DI extension method in Jira.RestApi | |
| 6 | (none) | -- | Update Fokus.API references and consumers | |
| 7 | (none) | -- | Clean up: remove old folder, update solution, verify build | |

Steps 3-7 have no matching skill because they are refactoring moves (extract interface, relocate files, update references), not pattern-based creation. No gap to log — these are one-time mechanical operations, not repeatable patterns.

## Domain Model Changes

None. This is a pure infrastructure refactoring.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Write `src/Modules/Jira/CLAUDE.md` [DONE]

**What:** Create the module axis file that the `create-module` skill reads.
**Skill:** Follow `create-module-claude-md` (Component template).
**Files created:**
- `src/Modules/Jira/CLAUDE.md`

### Step 2: Scaffold module skeleton

**What:** Run the `create-module` skill to create the two project folders, csproj files, and stub classes.
**Skill:** Follow `create-module` (ScaffoldComponent workflow).
**Depends on:** Step 1.
**Files to create:**
- `src/Modules/Jira/Jira.Contracts/Jira.Contracts.csproj`
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` (stub — replaced in Step 3)
- `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj`
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` (stub — replaced in Step 4)
- `src/Modules/Jira/Jira.RestApi/RestApiJiraOptions.cs` (stub — replaced in Step 4)
- `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs` (stub — replaced in Step 5)

**Post-scaffold adjustments the developer must make to the csproj files:**
- `Jira.Contracts.csproj`: Add `<ProjectReference>` to `Fokus.JiraContracts` (for return types in `IJiraClient`) and `<ProjectReference>` to `Fokus.Domain` (for `SprintState` enum).
- `Jira.RestApi.csproj`: Add `<PackageReference>` for `Refit.HttpClientFactory`. Add `<ProjectReference>` to `Fokus.JiraContracts` (for Jira response contracts used in `IJiraApi`). The reference to `Jira.Contracts` is already scaffolded by the skill.

### Step 3: Define `IJiraClient` interface in Contracts

**What:** Replace the stub `IJiraClient.cs` with the real interface extracted from `JiraClient`'s public method signatures. The interface uses types from `Fokus.JiraContracts` (response contracts) and `Fokus.Domain` (for `SprintState` enum).
**Depends on:** Step 2.
**Files to modify:**
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs`

**Methods to extract (from current `JiraClient` public surface):**
```
Task<List<JiraBoard>> GetBoardsAsync(CancellationToken ct)
Task<List<JiraSprint>> GetSprintsAsync(int boardId, CancellationToken ct, params SprintState[] states)
Task<List<JiraIssue>> GetSprintIssuesAsync(int sprintId, CancellationToken ct)
Task<List<JiraIssue>> GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct)
Task<List<JiraIssue>> GetEpicIssuesAsync(string epicKey, CancellationToken ct)
```

### Step 4: Move implementation files to `Jira.RestApi`

**What:** Move and rename the three implementation files from `Infrastructure/Jira/` into the new module.
**Depends on:** Step 3.
**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — content from `Infrastructure/Jira/JiraClient.cs`, renamed class to `RestApiJiraClient`, implements `IJiraClient`, update namespace to `Jira.RestApi`
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — content from `Infrastructure/Jira/IJiraApi.cs`, update namespace to `Jira.RestApi` (internal to the implementation — not exposed in Contracts)
- `src/Modules/Jira/Jira.RestApi/JiraOptions.cs` — content from `Infrastructure/Jira/JiraOptions.cs`, update namespace to `Jira.RestApi`

**Naming note:** `JiraOptions` keeps its name (not `RestApiJiraOptions`) because its properties (instance URL, email, API token) are generic Jira connection config, not implementation-specific. The config section name stays `"Jira"`. Delete the stub `RestApiJiraOptions.cs` created by the scaffold.

### Step 5: Wire DI in `Jira.RestApi/DependencyInjection.cs`

**What:** Replace the stub `DependencyInjection.cs` with the real registration logic currently in `Fokus.API/DependencyInjection.cs` (lines 22-43). The extension method `AddRestApiJira(IServiceCollection, IConfiguration)` registers:
1. `JiraOptions` from config section `"Jira"` with validation
2. The Refit client `IJiraApi` with `SystemTextJsonContentSerializer` and Basic Auth header
3. `RestApiJiraClient` as `IJiraClient` (transient)

**Depends on:** Step 4.
**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs`

**Pattern reference:** The current DI code in `Fokus.API/DependencyInjection.cs` lines 22-43 is the source. Move it verbatim into the extension method, adjusting type names (`JiraClient` -> `RestApiJiraClient`).

### Step 6: Update Fokus.API to consume the module

**What:** Switch Fokus.API from the old `Infrastructure/Jira/` references to the new module.
**Depends on:** Step 5.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj`:
  - Add `<ProjectReference>` to `src/Modules/Jira/Jira.Contracts/Jira.Contracts.csproj`
  - Add `<ProjectReference>` to `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj`
  - Remove `<PackageReference>` for `Refit.HttpClientFactory` (now owned by `Jira.RestApi`)
  - Keep `<ProjectReference>` to `Fokus.JiraContracts` (still needed — Fokus.API uses Jira response contracts directly in endpoint handlers)
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs`:
  - Remove the `using Fokus.API.Infrastructure.Jira;` import
  - Remove lines 22-43 (JiraOptions, Refit client, JiraClient registrations)
  - Add `using Jira.RestApi;`
  - Add `services.AddRestApiJira(configuration);` call
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs`:
  - Change `using Fokus.API.Infrastructure.Jira;` to `using Jira.Contracts;`
  - Change constructor parameter `JiraClient jiraClient` to `IJiraClient jiraClient`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`:
  - Change `using Fokus.API.Infrastructure.Jira;` to `using Jira.Contracts;`
  - Change constructor parameter `JiraClient jiraClient` to `IJiraClient jiraClient`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs`:
  - Change `using Fokus.API.Infrastructure.Jira;` to `using Jira.Contracts;`
  - Change constructor parameter `JiraClient jiraClient` to `IJiraClient jiraClient`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs`:
  - Change `using Fokus.API.Infrastructure.Jira;` to `using Jira.Contracts;`
  - Change constructor parameter `JiraClient jiraClient` to `IJiraClient jiraClient`

### Step 7: Remove old files, update solution, verify build

**What:** Delete the old `Infrastructure/Jira/` folder. Ensure new projects are in `Fokus.slnx`. Build to verify.
**Depends on:** Step 6.

**Files to delete:**
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/IJiraApi.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraClient.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraOptions.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/` (directory)

**Files to modify (if not already done by scaffold):**
- `src/Fokus.slnx` — ensure both new projects are under a `/Modules/Jira/` solution folder:
  ```xml
  <Folder Name="/Modules/">
    <Folder Name="/Modules/Jira/">
      <Project Path="Modules/Jira/Jira.Contracts/Jira.Contracts.csproj" />
      <Project Path="Modules/Jira/Jira.RestApi/Jira.RestApi.csproj" />
    </Folder>
  </Folder>
  ```

**Verification:** `dotnet build src/Fokus.slnx` must pass with zero errors.

## Cross-Service Changes

None. Single-service refactoring.

## Migration Notes

None. No data model changes.

## Testing Strategy

- **Build verification:** `dotnet build src/Fokus.slnx` passes after every step.
- **Runtime smoke test:** Start the API, call `GET /api/boards` — should return boards from Jira (same behavior as before).
- **Regression:** All 4 sync endpoints (`/api/boards`, `/api/jira/sprints`, `/api/sync/sprints`, `/api/sync/backlog`) should behave identically to pre-refactoring.
- **DI verification:** Confirm `IJiraClient` resolves to `RestApiJiraClient` at runtime (any sync endpoint call proves this).

## Open Questions

None — all decisions resolved:
- `SprintState` stays in `Fokus.Domain`; `Jira.Contracts` references `Fokus.Domain`
- `Fokus.JiraContracts` stays as BuildingBlock (circular dependency prevents absorption — see `questions.md` Q1)
- Module name: `Jira`
- Implementation project: `Jira.RestApi`
- Implementation class: `RestApiJiraClient` (skill convention `{ImplName}{ContractName}`)
- DI extension: `AddRestApiJira()`
- Jira response classes referred to as contracts (inter-system contract with Jira API)
