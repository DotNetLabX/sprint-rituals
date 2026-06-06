# Pass 3a — DomainEnrichment — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Sync/JiraDomainMapper.cs` — ADR-006 boundary mapper. Implements `IRegister` for 3 clean 1:1 Mapster maps (Sprint, Ticket, Developer) plus 2 plain static methods for the non-1:1 cases (StatusTransitions — one-to-many from changelog; SprintMembership — multi-source with planning-window business logic). Logic moved verbatim from the 5 Group-1 domain factories.
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayDomainMapper.cs` — Plain static mapper for 3 Group-2 primitive construction methods (CreateTestRun, CreateTestExecution, CreateTestSet). No Mapster `IRegister` — there is no source DTO type; these are pure field assignments from primitives/enums.
- `src/Services/Fokus/Fokus.Domain/Sprint/SprintState.cs` — Domain-owned `SprintState` enum (Active, Closed, Future). Required by the reference drop: `SprintState` was in `Jira.Contracts` and used directly as `Sprint.State`'s type in the domain. Moving it to the domain was necessary to satisfy ADR-006 without keeping the `Jira.Contracts` reference in `Fokus.Domain`.

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — Replaced 7 domain `From*` calls with boundary mapper calls: `Sprint.FromJira` → `JiraDomainMapper.MapSprint`, `Ticket.FromJira` (×2, in `SyncEpicDiscoveryAsync` + `SyncAsync`) → `JiraDomainMapper.MapTicket`, `Developer.FromJira` (×2) → `JiraDomainMapper.MapDeveloper`, `StatusTransition.ListFromJira` → `JiraDomainMapper.MapStatusTransitions`, `SprintMembership.FromJira` → `JiraDomainMapper.MapSprintMembership`.
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs` — Replaced 3 domain `From*` calls with boundary mapper calls: `TestExecution.FromXray` → `XrayDomainMapper.CreateTestExecution`, `TestRun.FromXray` → `XrayDomainMapper.CreateTestRun`, `TestSet.FromJiraIssueLink` → `XrayDomainMapper.CreateTestSet`. Added explicit `using Jira.Contracts;` (was previously resolved via `global using`).
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — Deleted `FromJira` factory; kept `AddMembership`.
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs` — Deleted `FromJira` factory; kept `GetEffectiveSp`.
- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — Removed `<ProjectReference>` for `Jira.Contracts.csproj`. Domain now depends only on `Blocks.Domain`.
- `src/Services/Fokus/Fokus.Domain/GlobalUsings.cs` — Removed `global using Jira.Contracts;`.
- `src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` — Removed `global using Jira.Contracts;` (Persistence had no actual Jira usage; the using was transitive through Domain).
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — Removed `global using Jira.Contracts;`. Reason: after adding `Fokus.Domain.SprintState`, the global import caused `SprintState` ambiguity across all Fokus.API files. Replaced with explicit per-file `using Jira.Contracts;` in the 8 files that need Jira types directly.
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs` — Added `using Jira.Contracts;`; qualified `SprintState` filter args as `Jira.Contracts.SprintState.Active/Closed` to disambiguate from `Fokus.Domain.SprintState`.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — Added `using Jira.Contracts;`; qualified `SprintState` filter args.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs` — Added `using Jira.Contracts;`; qualified `SprintState.Future` filter arg.
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintEndpoint.cs` — Added `using Jira.Contracts;` (uses `IJiraClient`).
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — Added `using Jira.Contracts;` (uses `IJiraClient` and `JiraIssue`).
- `src/Services/Fokus/Fokus.API/Features/Sync/JiraDomainMapper.cs` — Added `using Jira.Contracts;` (uses `JiraSprint`, `JiraIssue`); qualified `SprintState` enum values as `Fokus.Domain.SprintState.*` to resolve ambiguity.
- `src/Services/Fokus/CLAUDE.md` — Corrected "Sync-from-external" line: replaced the false `JiraMapper` claim with the real boundary mapping description (`JiraDomainMapper` + `XrayDomainMapper`; ADR-006 rule stated).

## Files Deleted

- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` — contained only the deleted `Ticket.FromJira` factory; file became empty.
- `src/Services/Fokus/Fokus.Domain/Developer/Behaviors/Developer.cs` — contained only the deleted `Developer.FromJira` factory; file became empty.
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/StatusTransition.cs` — contained only the deleted `StatusTransition.ListFromJira` factory; file became empty.
- `src/Services/Fokus/Fokus.Domain/TestRun/Behaviors/TestRun.cs` — contained only the deleted `TestRun.FromXray` factory; file became empty.
- `src/Services/Fokus/Fokus.Domain/TestExecution/Behaviors/TestExecution.cs` — contained only the deleted `TestExecution.FromXray` factory; file became empty.
- `src/Services/Fokus/Fokus.Domain/TestSet/Behaviors/TestSet.cs` — contained only the deleted `TestSet.FromJiraIssueLink` factory; file became empty.

## Key Decisions

- **StatusTransition and SprintMembership as plain static methods, not Mapster `MapWith`:** These are one-to-many (changelog iteration) and multi-source (multi-arg) shapes. Mapster `MapWith` with a tuple source is feasible but adds no value over a plain static method on the same class. Plan explicitly sanctioned developer's call here.
- **Group-2 method names `Create*` not `From*`:** `CreateTestRun`, `CreateTestExecution`, `CreateTestSet` — drops the misleading `FromDTO` framing (these take only primitives). Plan flagged this as developer's call.
- **`SprintState` moved to `Fokus.Domain`:** Plan said "drop the `Jira.Contracts` reference" without noting that `Sprint.State` was typed as `Jira.Contracts.SprintState`. Moving the enum to `Fokus.Domain/Sprint/SprintState.cs` was the only way to satisfy ADR-006. `Jira.Contracts.SprintState` remains in the Jira module for `IJiraClient.GetSprintsAsync` filter params; the two are now separate types with the same shape. The `Fokus.API` files that pass the filter use `Jira.Contracts.SprintState.*` (fully qualified where ambiguous); all domain-level comparisons (`s.State == SprintState.Closed`) resolve to `Fokus.Domain.SprintState` via `global using Fokus.Domain;`.
- **`global using Jira.Contracts;` removed from `Fokus.API/GlobalUsings.cs`:** Required once `Fokus.Domain.SprintState` existed — the global import caused ambiguity across all 35+ Fokus.API files. Replaced with 8 per-file explicit usings.

## Carry-Over Findings

| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| SprintState duplication (Domain + Jira.Contracts) | low | architect | `Fokus.Domain/Sprint/SprintState.cs` and `Jira.Contracts/SprintState.cs` are now two identical enums | Intentional split: one is a domain state concept, one is a Jira API filter; both will diverge over time as domain state enriches. Consider documenting in ADR-006 addendum. |

## KB Changes

None. No `docs/kb/` exists; no business rule changed.

## Deviations from Plan

- **`SprintState` moved to `Fokus.Domain`** — Plan did not mention this but the reference drop required it. `Sprint.State` was typed `Jira.Contracts.SprintState`; without moving it the domain would still depend on `Jira.Contracts`. This is within the ADR-006 intent (no external type in the domain).
- **`global using Jira.Contracts;` removed from `Fokus.API/GlobalUsings.cs`** — Not in the plan. Required by the `SprintState` move to eliminate widespread ambiguity. All 8 affected files got explicit `using Jira.Contracts;`; the analytics endpoints that only use domain-side `SprintState` comparisons are unaffected (they pick up `Fokus.Domain.SprintState` via `global using Fokus.Domain;`).
