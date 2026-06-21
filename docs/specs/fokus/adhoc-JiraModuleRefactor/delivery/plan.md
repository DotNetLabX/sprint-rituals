# JiraModuleRefactor

**Feature Spec:** None

## Context

The previous `ExtractJiraModule` refactoring extracted the Jira HTTP client into `src/Modules/Jira/` but left `Fokus.JiraContracts` as a standalone BuildingBlock because a circular dependency blocked absorption. The circular dependency is:

```
Jira.Contracts → Fokus.Domain (for SprintState enum)
Fokus.Domain → Fokus.JiraContracts (for FromJira() factory methods)
Jira.Contracts → Fokus.JiraContracts (for return types)
```

If `Fokus.JiraContracts` were merged into `Jira.Contracts`, then `Fokus.Domain` would reference `Jira.Contracts`, but `Jira.Contracts` already references `Fokus.Domain` — cycle.

This refactoring breaks the cycle by moving `SprintState` from `Fokus.Domain` to `Jira.Contracts`, eliminating `Jira.Contracts`'s dependency on `Fokus.Domain`. Once that dependency is gone, `Fokus.JiraContracts` types can be merged into `Jira.Contracts` and the BuildingBlock project removed.

## Scope

**In scope:**
- Move `SprintState` enum from `Fokus.Domain` to `Jira.Contracts`
- Merge all `Fokus.JiraContracts` types (5 files) into `Jira.Contracts`
- Remove `Fokus.JiraContracts` project from all csproj references and solution file
- Delete the `src/BuildingBlocks/Fokus.JiraContracts/` project
- Update all `using Fokus.JiraContracts` and `using Fokus.Domain` (for SprintState) to `using Jira.Contracts`
- Update `Jira.Contracts.csproj` to remove both project references (`Fokus.JiraContracts`, `Fokus.Domain`)
- Update `src/Modules/Jira/CLAUDE.md` to reflect the new structure

**Out of scope:**
- Changing any Jira API method signatures or behavior
- Changing any domain model logic
- Adding new types or features
- Renaming classes (only namespace changes)

## Dependency Graph (after refactor)

```
Jira.Contracts → (nothing)
Jira.RestApi → Jira.Contracts, Refit
Fokus.Domain → Blocks.Domain, Jira.Contracts
Fokus.Persistence → Fokus.Domain, Blocks.EntityFrameworkCore, Blocks.FastEndpoints
Fokus.API → Fokus.Domain, Fokus.Persistence, Jira.Contracts, Jira.RestApi, Blocks.AspNetCore
```

No cycles. `Fokus.JiraContracts` is eliminated entirely.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Move SprintState enum, change namespace | |
| 2 | (none) | — | Move 5 DTO files, change namespaces | |
| 3 | (none) | — | Update csproj references across 5 projects | |
| 4 | (none) | — | Update using directives across ~10 files | |
| 5 | (none) | — | Remove project, update solution file, update CLAUDE.md | |
| 6 | (none) | — | Build verification | |

All steps are one-time mechanical refactoring operations (move files, update namespaces, fix references). No repeatable pattern warrants a skill.

## Domain Model Changes

None. `SprintState` changes namespace from `Fokus.Domain` to `Jira.Contracts` but remains the same enum used by `Sprint.State`.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Move `SprintState` to `Jira.Contracts`

**What:** Move the `SprintState` enum from `Fokus.Domain` to `Jira.Contracts`. Change its namespace to `Jira.Contracts`.
**Depends on:** Nothing.
**Files to create:**
- `src/Modules/Jira/Jira.Contracts/SprintState.cs` — the enum with namespace `Jira.Contracts`

**Files to delete:**
- `src/Services/Fokus/Fokus.Domain/Enums/SprintState.cs`

### Step 2: Merge `Fokus.JiraContracts` types into `Jira.Contracts`

**What:** Move all 5 source files from `Fokus.JiraContracts` into `Jira.Contracts`, changing their namespace from `Fokus.JiraContracts` to `Jira.Contracts`.
**Depends on:** Step 1.
**Files to create (in `src/Modules/Jira/Jira.Contracts/`):**
- `JiraBoard.cs`
- `JiraSprint.cs`
- `JiraIssue.cs`
- `JiraChangelog.cs`
- `JiraPagedResult.cs`

These are copies of the files from `src/BuildingBlocks/Fokus.JiraContracts/` with namespace changed to `Jira.Contracts`.

### Step 3: Update project references

**What:** Remove all references to `Fokus.JiraContracts` project. Add `Jira.Contracts` reference where needed. Remove `Fokus.Domain` reference from `Jira.Contracts`.
**Depends on:** Step 2.

**Files to modify:**

- `src/Modules/Jira/Jira.Contracts/Jira.Contracts.csproj`:
  - Remove `<ProjectReference>` to `Fokus.JiraContracts`
  - Remove `<ProjectReference>` to `Fokus.Domain`
  - (Project should have zero project references after this step)

- `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj`:
  - Remove `<ProjectReference>` to `Fokus.JiraContracts`
  - (Already has reference to `Jira.Contracts` — that's sufficient now)

- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj`:
  - Remove `<ProjectReference>` to `Fokus.JiraContracts`
  - Add `<ProjectReference>` to `Jira.Contracts` (for JiraBoard, JiraSprint, JiraIssue types used in FromJira() methods, and for SprintState enum)

- `src/Services/Fokus/Fokus.API/Fokus.API.csproj`:
  - Remove `<ProjectReference>` to `Fokus.JiraContracts`
  - (Already has reference to `Jira.Contracts` — that's sufficient)

- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj`:
  - No changes needed (references `Fokus.Domain`, not `Fokus.JiraContracts`)

### Step 4: Update using directives

**What:** Replace all `using Fokus.JiraContracts;` with `using Jira.Contracts;`. Replace `using Fokus.Domain;` with `using Jira.Contracts;` where it was only used for `SprintState`. Files that use both `Fokus.Domain` types and `SprintState`/Jira types need both `using Fokus.Domain;` and `using Jira.Contracts;`.
**Depends on:** Step 3.

**Files to modify:**

In `Jira.RestApi`:
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — remove `using Fokus.JiraContracts;`, remove `using Fokus.Domain;` (SprintState now comes from `Jira.Contracts` which is already referenced via the project)
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — remove `using Fokus.JiraContracts;`, add `using Jira.Contracts;`

In `Jira.Contracts`:
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — remove `using Fokus.Domain;`, remove `using Fokus.JiraContracts;` (all types now in the same namespace)

In `Fokus.Domain` (behaviors that use Jira DTOs):
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — change `using Fokus.JiraContracts;` to `using Jira.Contracts;`
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs` — change `using Fokus.JiraContracts;` to `using Jira.Contracts;`
- `src/Services/Fokus/Fokus.Domain/Developer/Behaviors/Developer.cs` — change `using Fokus.JiraContracts;` to `using Jira.Contracts;`
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` — change `using Fokus.JiraContracts;` to `using Jira.Contracts;`
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/StatusTransition.cs` — change `using Fokus.JiraContracts;` to `using Jira.Contracts;`

In `Fokus.API` (endpoints that use SprintState — already have `using Fokus.Domain;` which still provides other types):
- Check if any endpoint file has `using Fokus.JiraContracts;` — grep showed none do, so no changes needed in API endpoint files.

### Step 5: Remove `Fokus.JiraContracts` project and update documentation

**What:** Delete the BuildingBlock project. Remove it from the solution file. Update the Jira module CLAUDE.md.
**Depends on:** Step 4.

**Files to delete:**
- `src/BuildingBlocks/Fokus.JiraContracts/` (entire directory — `Fokus.JiraContracts.csproj`, `JiraBoard.cs`, `JiraSprint.cs`, `JiraIssue.cs`, `JiraChangelog.cs`, `JiraPagedResult.cs`)

**Files to modify:**
- `src/Fokus.slnx` — remove the `Fokus.JiraContracts` project entry from the `/BuildingBlocks/` folder
- `src/Modules/Jira/CLAUDE.md` — update to reflect that Jira API contracts now live in `Jira.Contracts` (not a separate BuildingBlock), and `SprintState` is in `Jira.Contracts` (not `Fokus.Domain`). Remove references to `Fokus.JiraContracts`.

### Step 6: Build verification

**What:** Run `dotnet build src/Fokus.slnx` and verify zero errors, zero warnings related to missing types or references.
**Depends on:** Step 5.

## Cross-Service Changes

None. Single-service refactoring within the Fokus solution.

## Migration Notes

None. No data model changes.

## Testing Strategy

- **Build verification:** `dotnet build src/Fokus.slnx` passes with zero errors after each step.
- **Namespace verification:** No remaining references to `Fokus.JiraContracts` namespace or project anywhere in the solution.
- **No remaining `SprintState` in `Fokus.Domain`:** Grep confirms `SprintState` is only defined in `Jira.Contracts`.
- **Runtime smoke test:** Start the API, call any sync endpoint to confirm Jira client still resolves and works.

## Open Questions

None.
