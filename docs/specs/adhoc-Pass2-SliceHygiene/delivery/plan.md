# Pass 2 — SliceHygiene

**Feature Spec:** None (adhoc refactoring pass — scope in `docs/backlog.md` row 2 + `docs/architecture/decisions.md` ADR-008/009/010/013; user decisions in `docs/specs/adhoc-Pass2-SliceHygiene/delivery/questions.md` Q1–Q5, all Answered — **MAXIMAL scope, Path A**).

## Context

Pass 2 is the slice-hygiene arm of the refactor program. It applies the skills repaired/created in Pass 1 (`extract-endpoint-types`, `central-package-management`) to the FastEndpoints feature slices that violate ADR-008/009/010. The codebase shipped before the skill-enforcement guards existed (`refactor-triage.md` "Root cause"), so request/response/validator types are crammed into endpoint files, named with the `*Request` suffix, and mapped by hand. This pass fixes that **problem class** (`.claude/rules/engineering-discipline.md`): fix every instance, not the cited examples.

**Two services impacted:** `Fokus.API`, `Identity.API` (the only modules with FastEndpoints feature slices). Plus three contract types outside `Features/` brought into scope by the user's Q1 answer (gRPC `Blocks.Contracts`, Jira `Jira.Contracts`). Plus the Vue client (`client/`), brought in by the Q2 Path-A field renames.

**Binding decisions baked in (questions.md):**
- **Q1 — MAXIMAL:** rename gRPC `DataContracts/*Request` and Jira `*Request` too. Architect has verified wire stability (see Cross-Service Changes — `[DataMember(Order)]` keys the protobuf wire, not the C# type name).
- **Q2/Q3 — Path A:** rename request **fields** to match the entity, update the Vue client, AND apply Mapster to the `Save*` settings commands now against today's flat `AppSettings` (Pass 3 VO rework acknowledged).
- **Q4a:** core `Mapster` ONLY in CPM (no `Mapster.DependencyInjection`); per-module DI via `TypeAdapterConfig.GlobalSettings.Scan(...)`; `IRegister` maps beside their slice.
- **Q4b:** `class`→`record` conversion opportunistic, non-blocking.
- **Q5:** client is hand-written TS (verified — `client/src/api/*.ts` uses `apiFetch` + `JSON.stringify`, no generated client); client changes are in scope for field renames only.

## Scope

**In scope:**
1. Add `Mapster` to CPM (`src/Directory.Packages.props`) + per-module DI registration (ADR-013 prerequisite + ADR-010 enabler). **First step.**
2. Split every fat FastEndpoints file (request/response/DTO/validator inline in `*Endpoint.cs`) into `{Feature}Endpoint.cs` + `{Feature}Command.cs`/`{Feature}Query.cs` (ADR-008). **16 fat files.**
3. Rename `*Request` → `{Action}{Entity}Command` (mutations) / `Query` (reads) and `*RequestValidator` → `*CommandValidator`/`*QueryValidator` (ADR-009). **~29 FastEndpoints types + 2 gRPC + 1 Jira.**
4. Path-A field renames: align mismatched request fields to entity names; update the Vue client payloads (ADR-010, Q2).
5. Apply Mapster (`IRegister` + `.Adapt<T>()`) wherever a handler hand-maps between matching-shaped objects — including the `Save*` settings commands (Q3).
6. Normalize non-standard sibling filenames (`*Models.cs`) to `*Command.cs`/`*Query.cs`.

**Explicitly out of scope:**
- Pass 3 work: introducing value objects on `AppSettings`/aggregates, domain services, no-DTO-in-domain. Pass 2 maps against **today's flat** entities.
- `SendOkAsync`→`Send.*` migration (Pass 0, done — verified `Send.*` is already used).
- CPM setup itself (Pass 0, done — only the Mapster *addition* is new).
- Domain-event dispatch (Pass 5), Identity extraction (Pass 4).
- Renaming non-`*Request` types, route paths, or HTTP verbs. Routes and JSON wire keys stay identical **except** where Path-A field renames change them (Step 6 — enumerated).

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | `central-package-management` | Follow | Add `<PackageVersion Include="Mapster" Version="{latest stable 7.x}"/>`; bare `<PackageReference Include="Mapster"/>` in `Fokus.API.csproj` + `Identity.API.csproj` | — |
| 2 | (none) | — | Mapster DI registration per module (`TypeAdapterConfig.GlobalSettings.Scan` of the API assembly) in each `Add{Module}Module` | Log to lessons (no skill covers Mapster DI bootstrap) |
| 3 | `extract-endpoint-types` | Follow | 16 fat files (listed in step); split request+response+DTO+validator into `{F}Command.cs`/`{F}Query.cs` | — |
| 4 | `extract-endpoint-types` | Follow | ~29 type renames `*Request`→`Command`/`Query` + `*RequestValidator`→`*CommandValidator`/`*QueryValidator`; normalize `*Models.cs`→`*Command.cs`/`*Query.cs` | — |
| 5 | (none) | — | gRPC `TeamRequest`/`SprintRequest` + Jira `UpdateJiraSprintRequest` renames; wire-stability verification | Log to lessons (extract-endpoint-types is FastEndpoints-only; gRPC/Jira are out of its domain) |
| 6 | (none) | — | Path-A field renames (entity-aligned) + matching Vue client payload edits; enumerated mismatches only | Log to lessons (cross-stack rename, no skill) |
| 7 | (none) | — | Apply Mapster `IRegister` + `.Adapt<T>()` to the trivial-map handlers, including `Save*` settings | Log to lessons (no Mapster-application skill — ADR-010 candidate) |
| 8 | (none) | — | Verification sweep (grep-checkable acceptance) + build (backend + client) | — |

**Disposition notes:**
- **Step 3/4 Follow `extract-endpoint-types`** — the skill governs the file-split and the Command/Query naming. The plan supplies only the file list and the rename map; the developer invokes the skill for the split mechanics (what moves, `using` carry-over, sibling naming). Do not restate the split procedure.
- **Steps 2, 5, 6, 7 are None** — no skill covers Mapster DI bootstrap, gRPC/Jira contract renaming (the skill is FastEndpoints-scoped), cross-stack field renames, or applying Mapster to handlers. These are logged as skill gaps for the learner; ADR-010 application is the strongest future-skill candidate.
- **Mapster is core-only (Q4a).** There is NO `Mapster.DependencyInjection`, so registration is `TypeAdapterConfig.GlobalSettings.Scan(typeof(SomeTypeInAssembly).Assembly)` in each module's `Add{Module}Module`, not `services.AddMapster()`. `.Adapt<T>()` uses `GlobalSettings` implicitly — no `IMapper` injection.

## Domain Model Changes

None. Pass 2 does not touch aggregates, entities, value objects, or domain events. It maps against the **current flat** `AppSettings` and other entities. (Pass 3 reshapes the domain; that rework is acknowledged per Q3.)

## Data Model Changes

None. No migrations, no schema changes. Type renames and file splits do not affect EF mappings (configs reference entity types, not request/response types).

## Implementation Steps

**Ordering (binding):** Step 1 (Mapster→CPM) is first — Step 7 cannot apply Mapster until the package resolves. Steps 3–4 (split + rename) before Step 6 (field renames) before Step 7 (Mapster application) — you map the renamed, aligned types, not the originals. Step 5 (gRPC/Jira) is independent and may run any time after Step 1. Step 8 verifies last.

### Step 1 — Add Mapster to Central Package Management (prerequisite)

**Disposition: Follow `central-package-management`.** **Confidence: high.**

Add the core Mapster package to CPM and reference it (bare) in the two API projects that will host `IRegister` maps.

- **Modify** `src/Directory.Packages.props` — add `<PackageVersion Include="Mapster" Version="{latest stable}"/>` under the "Other third-party" group (verify latest stable per `framework-currency`; Mapster is currently 7.x).
- **Modify** `src/Services/Fokus/Fokus.API/Fokus.API.csproj` and `src/Services/Identity/Identity.API/Identity.API.csproj` — add bare `<PackageReference Include="Mapster" />` (no `Version`).
- **Do NOT** add `Mapster.DependencyInjection` (Q4a).

**Accept:**
- `rg -n 'Mapster' --glob '*.props' src/` shows exactly one `<PackageVersion Include="Mapster"`.
- Three-form grep stays clean: `rg -n 'Version="' --glob '*.csproj' src/`, `rg -n '<Version>' --glob '*.csproj' src/`, `rg -n 'VersionOverride=' --glob '*.csproj' src/` all return zero hits (the bare Mapster references carry no version).
- `rg -n 'Mapster.DependencyInjection' src/` returns zero hits.
- `dotnet build SprintRituals.slnx` restores Mapster with no error.

### Step 2 — Register Mapster per module (DI bootstrap)

**Disposition: None** (no skill covers core-Mapster DI bootstrap). **Confidence: medium** — the pattern is fixed by Q4a but has no skill; reference the existing service-registration shape in each `DependencyInjection.cs`.

Register Mapster's global config in each module's `Add{Module}Module` so `IRegister` implementations in that assembly are discovered. Use `TypeAdapterConfig.GlobalSettings.Scan(...)` against the module's API assembly (core Mapster, no `AddMapster`). Place the call inside the existing extension method — do not add a new wiring location (ADR-014: module owns its wiring).

- **Modify** `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — in `AddFokusModule`, scan the `Fokus.API` assembly into `TypeAdapterConfig.GlobalSettings`.
- **Modify** `src/Services/Identity/Identity.API/DependencyInjection.cs` — in `AddIdentityModule`, scan the `Identity.API` assembly.

Domain constraints: scan must run once at startup; `GlobalSettings` is process-wide so both modules contribute to the shared config (no conflict — maps are keyed by source/target type pairs). No `IMapper` is registered (handlers call `.Adapt<T>()` which reads `GlobalSettings`).

**Accept:**
- `rg -n 'GlobalSettings.Scan' src/Services/Fokus/Fokus.API/DependencyInjection.cs src/Services/Identity/Identity.API/DependencyInjection.cs` returns one hit per file.
- `rg -n 'AddMapster' src/` returns zero hits (core-only, Q4a).
- `dotnet build SprintRituals.slnx` green.

### Step 3 — Split fat FastEndpoints files (ADR-008)

**Disposition: Follow `extract-endpoint-types`.** **Confidence: high** (clear precedent — most features already split, e.g. `GetBugRatio`).

Split each file below so `{Feature}Endpoint.cs` holds only the endpoint class + handler, and a new sibling holds request + response + DTOs + validator. Mutating endpoints → `{Feature}Command.cs`; read endpoints → `{Feature}Query.cs`. For `EndpointWithoutRequest<T>` features (no request type), the sibling holds response + DTOs (name it `{Feature}Query.cs` — they are reads).

A "fat file" here is an `*Endpoint.cs` that **defines** a `*Request`/`*Response`/`*Validator`/`*Dto` type inline. The verified inventory is **16** (definition-line grep, not usage — `GetMe` and `Logout` are NOT fat: `GetMe` already keeps `MeResponse` in its `GetMeQuery.cs` sibling, `Logout` defines no type; `SetCapacity` is already split — its endpoint file is clean).

**Fat files to split (16 — verified inventory):**

Settings — mutating → `*Command.cs` (10): `SaveBugRatioAlerts`, `SaveBoard`, `SaveSyncConfig`, `SaveAnalyticsTargets`, `SaveHealthConfig`, `SaveCycleTimeBoundaries`, `SaveXraySettings`, `SaveExcludedStatuses`, `SaveDoneStatuses`, `SaveWorkflowStages` — all under `src/Services/Fokus/Fokus.API/Features/Settings/{Feature}/{Feature}Endpoint.cs`.

Settings — read → `*Query.cs` (1):
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs` (`EndpointWithoutRequest` → `GetCycleTimeBoundariesQuery.cs` holds the `GetCycleTimeBoundariesResponse` record). **Note:** this is the GET/read response — distinct from `SaveCycleTimeBoundariesResponse` (the PUT response in the `SaveCycleTimeBoundaries` slice). Leave its `StartStage`/`EndStage` keys unchanged (read contract; not a Path-A field rename).

Other Fokus (4):
- `src/Services/Fokus/Fokus.API/Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs` (mutating → Command).
- `src/Services/Fokus/Fokus.API/Features/Team/GetTeamRoster/GetTeamRosterEndpoint.cs` (`EndpointWithoutRequest` → `GetTeamRosterQuery.cs` holds `TeamDeveloperDto` + `TeamRosterResponse`).
- `src/Services/Fokus/Fokus.API/Features/Developers/GetDevelopers/GetDevelopersEndpoint.cs` (`EndpointWithoutRequest<List<DeveloperDto>>` → `GetDevelopersQuery.cs` holds `DeveloperDto`). **Note:** the endpoint returns a bare `List<DeveloperDto>` — there is NO `*Response` wrapper; the split only moves `DeveloperDto`. Do not invent a wrapper response.
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` (mutating → Command).
- `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionEndpoint.cs` (`EndpointWithoutRequest` → `TestConnectionQuery.cs` holds `TestConnectionResponse`).

Carry over only the `using`s the moved types need (e.g. `SyncXrayEndpoint.cs` has `using Jira.RestApi;`/`Microsoft.Extensions.Options;` used by the handler — keep those in the endpoint file; the moved types need none beyond the shared namespace).

**Accept:**
- For each of the 16 features, two files exist: `{Feature}Endpoint.cs` and a `{Feature}Command.cs` or `{Feature}Query.cs` sibling.
- `rg -n '^\s*(public\s+)?(class|record)\s+\w+(Request|Response|Validator|Dto)\b' --glob '*Endpoint.cs' src/` returns zero hits (no request/response/DTO/validator type is **defined** in any `*Endpoint.cs`). This covers already-split files too, so the whole sweep must be clean. (Definition-line pattern — `EndpointWithoutRequest<MeResponse>` usage lines do not match.)
- `dotnet build SprintRituals.slnx` green after the splits.

### Step 4 — Rename FastEndpoints request types + validators (ADR-009)

**Disposition: Follow `extract-endpoint-types`.** **Confidence: high.**

Rename every FastEndpoints feature request type from `{X}Request` to `{Action}{Entity}Command` (mutations) / `{Action}{Entity}Query` (reads), and its validator from `{X}RequestValidator`/`{X}Validator` to `{NewName}Validator`. The command/query record IS the request body — no `Request` suffix. Update the `Endpoint<TRequest,TResponse>` generic arg, the `HandleAsync` parameter type, and the `Validator<T>` generic arg at each site. Also normalize non-standard sibling filenames.

**Rename map (verified — ~29 types):**

Identity (`src/Services/Identity/Identity.API/Features/Auth/`):
- `UpdateRoleRequest`→`UpdateRoleCommand`, `UpdateRoleRequestValidator`→`UpdateRoleCommandValidator`.
- `UpdateStatusRequest`→`UpdateStatusCommand`, `UpdateStatusRequestValidator`→`UpdateStatusCommandValidator`.

Fokus mutations → `*Command` (`src/Services/Fokus/Fokus.API/Features/`):
- `SyncXrayRequest`→`SyncXrayCommand`; `UpdateTeamConfigRequest`→`UpdateTeamConfigCommand` (validator `UpdateTeamConfigValidator` → `UpdateTeamConfigCommandValidator`); `SetCapacityRequest`→`SetCapacityCommand`; `UpdateSprintRequest`→`UpdateSprintCommand` (validator `UpdateSprintValidator`→`UpdateSprintCommandValidator`); `CreateInvitationRequest`→`CreateInvitationCommand`; `RevokeInvitationRequest`→`RevokeInvitationCommand`.
- Settings: `SaveXraySettingsRequest`→`SaveXraySettingsCommand`, `SaveBugRatioAlertsRequest`→`SaveBugRatioAlertsCommand`, `SaveExcludedStatusesRequest`→`SaveExcludedStatusesCommand`, `SaveDoneStatusesRequest`→`SaveDoneStatusesCommand`, `SaveCycleTimeBoundariesRequest`→`SaveCycleTimeBoundariesCommand`, `SaveSyncConfigRequest`→`SaveSyncConfigCommand`, `SaveWorkflowStagesRequest`→`SaveWorkflowStagesCommand`, `SaveHealthConfigRequest`→`SaveHealthConfigCommand`, `SaveBoardRequest`→`SaveBoardCommand`, `SaveAnalyticsTargetsRequest`→`SaveAnalyticsTargetsCommand` (+ each `*RequestValidator`→`*CommandValidator`).

Fokus reads → `*Query`:
- All Analytics `Get*Request`→`Get*Query` (+ `*RequestValidator`→`*QueryValidator`): `GetBugRatio`, `GetCycleTime`, `GetCarryOver`, `GetDeveloperQuality`, `GetDeveloperDetail`, `GetDeveloperProgress`, `GetQaMetrics`, `GetSprintSummary`, `GetQaTrends`, `GetFailingTickets`, `GetLeaderboard`, `GetDeveloperThroughput`, `GetScopeChange`, `GetQaWorkload`, `GetEpicProgress`, `GetUntestedTickets`.
- `GetCapacityRequest`→`GetCapacityQuery`; `GetTestTimelineRequest`→`GetTestTimelineQuery` (+ `GetTestTimelineRequestValidator`→`GetTestTimelineQueryValidator`); `ValidateInvitationRequest`→`ValidateInvitationQuery`.

Filename normalization (the sibling already exists under a non-standard name):
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintModels.cs` → `UpdateSprintCommand.cs` (and fold `UpdateSprintValidator.cs` into it per ADR-008 two-file layout, or keep validator co-located — developer's call; the binding rule is the sibling is named `*Command.cs`).
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetSprints/GetSprintsModels.cs` → `GetSprintsQuery.cs`.

**Naming is a binding contract** (ADR-009) — these exact type names are the public-surface decision. Internal helper names are the developer's call.

**Accept:**
- `rg -n 'class \w+Request\b|record \w+Request\b' --glob '*.cs' src/Services/Fokus/Fokus.API/Features src/Services/Identity/Identity.API/Features` returns zero hits.
- `rg -n 'RequestValidator' --glob '*.cs' src/Services` returns zero hits.
- `rg -n '\*Models\.cs' ...` n/a — instead: `Get-ChildItem` shows no `*Models.cs` remains under the two `Features/` trees (`rg --files src/Services -g '*Models.cs'` returns nothing).
- `dotnet build SprintRituals.slnx` green.

### Step 5 — Rename gRPC + Jira `*Request` contracts (Q1, MAXIMAL) — verify wire stability

**Disposition: None** (`extract-endpoint-types` is FastEndpoints-only; these are gRPC code-first + Refit contracts). **Confidence: medium** — the rename is mechanical but the wire-stability verification is the load-bearing part.

Rename the three non-FastEndpoints `*Request` contract types and all their references. **The C# type name must change without changing the wire contract.**

- **gRPC** (`src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/`): `TeamRequest`→`TeamQuery`, `SprintRequest`→`SprintQuery`. Rename the files too (`TeamRequest.cs`→`TeamQuery.cs`, `SprintRequest.cs`→`SprintQuery.cs`).
  - **Wire-stability rule (binding):** protobuf-net.Grpc keys the wire format on `[DataMember(Order = N)]`, NOT the C# type or member name. Leave every `[DataContract]`, `[DataMember(Order=…)]`, and member declaration order **unchanged**. Renaming the class is wire-safe iff the `Order` numbers and member set are untouched. **Do not reorder, add, or renumber members.**
  - Update references: `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintCatalogService.cs`, `ISprintAnalyticsService.cs`, and the server impls `src/Services/Fokus/Fokus.API/Grpc/SprintCatalogGrpcService.cs`, `SprintAnalyticsGrpcService.cs`. (No gRPC *client* registration exists yet — the Reflekt port is future, per `Fokus/CLAUDE.md` — so there are no further consumers.)
- **Jira** (`src/Modules/Jira/Jira.Contracts/`): `UpdateJiraSprintRequest`→`UpdateJiraSprintCommand`. Rename the file.
  - **Wire-stability rule:** this is a Refit `[Body]` DTO serialized by **property name** to the Jira REST API. Renaming the **type** does not change the JSON keys (Refit serializes properties, not the type name). Leave property names unchanged.
  - Update references: `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` (the `[Body]` parameter type) and `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs:63` (the `new UpdateJiraSprintRequest { ... }` construction).

If any member-order or `[DataMember(Order)]` change appears necessary to make the rename compile, STOP and flag it — that would be a breaking wire change, not a rename (questions.md to architect).

**Accept:**
- `rg -n 'TeamRequest|SprintRequest|UpdateJiraSprintRequest' src/` returns zero hits.
- `rg -n 'DataMember\(Order' src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/` shows the same Order numbers as before the rename (TeamQuery: Order 1; SprintQuery: Order 1, 2) — git diff on these two files shows ONLY the type-name line changed, no `Order=` or member-order change.
- `dotnet build SprintRituals.slnx` green.

### Step 6 — Path-A field renames + Vue client updates (Q2)

**Disposition: None** (cross-stack rename, no skill). **Confidence: medium** — the C# side is mechanical; the client side requires matching the exact camelCase JSON keys and avoiding false-positive collisions.

Rename request **fields** to match the `AppSettings` entity field names so the maps become trivial (Step 7), and update the Vue client payloads to send the new JSON keys. **Only the enumerated mismatches** — fields that already match the entity are untouched.

**Field renames (verified mismatches — entity is `Fokus.Domain.AppSettings`):**

1. `SaveBugRatioAlertsCommand` (was `SaveBugRatioAlertsRequest`):
   - `AlertThreshold` → `BugRatioAlertThreshold`
   - `ConsecutiveSprintCount` → `BugRatioConsecutiveSprintCount`
   - (`DefaultSpPerBug` already matches — leave it.)
   - Update the validator's `RuleFor(x => x.…)` lambdas to the new names.
2. `SaveCycleTimeBoundariesCommand` (was `SaveCycleTimeBoundariesRequest`):
   - `StartStage` → `CycleTimeStartStage`
   - `EndStage` → `CycleTimeEndStage`
   - Update the validator and the handler's `AddError(r => r.StartStage, …)` / `r.EndStage` references (this handler has stage-order business logic — Step 7 will NOT Mapster it; only the field names change here).

All other `Save*` commands already have entity-matching field names (`SaveBoardCommand.BoardId`, `SaveSyncConfigCommand.{SyncBackSprintCount,PlanningWindowDays}`, `SaveAnalyticsTargetsCommand.BugRatioTarget`, `SaveHealthConfigCommand.{HealthThresholds,HealthWeights,QaHealthThresholds,QualityHealthWeight,QualitySubScoreWeights}`, `SaveXraySettingsCommand.{XrayEnabled,XrayClientId,XrayClientSecret}`) — **no field rename needed**.

**Vue client blast radius (hand-written TS — verified, no generated client):**

The wire JSON keys are camelCase (ASP.NET default). When the C# field becomes `BugRatioAlertThreshold`, the JSON key the client must send becomes `bugRatioAlertThreshold`. Affected files:
- `client/src/api/settings.ts`:
  - `saveBugRatioAlerts(...)` — the `JSON.stringify({ alertThreshold, consecutiveSprintCount, defaultSpPerBug })` payload keys must become `{ bugRatioAlertThreshold, bugRatioConsecutiveSprintCount, defaultSpPerBug }`. Rename the function parameters too for clarity, or remap at the call.
  - `saveCycleTimeBoundaries(...)` — the `JSON.stringify({ startStage, endStage })` payload keys must become `{ cycleTimeStartStage, cycleTimeEndStage }`.
- `client/src/stores/settingsStore.ts:132-137` — `saveBugRatioAlertsAction` passes args through to `saveBugRatioAlerts`; update to match the new param/payload shape. (The `settings.value = { ... bugRatioAlertThreshold: ... }` local-state assignment already uses the entity names — it reads the `AppSettings` GET response, which is unaffected.)
- The Vue components that CALL these store actions (the Settings tabs) — verify the call sites compile after any param rename: search `saveBugRatioAlerts`, `saveCycleTimeBoundaries` usages under `client/src/components/settings/` and `client/src/views/SettingsView.vue`.

**Collision warning (do NOT over-replace):** `consecutiveSprintCount` also appears at `client/src/types/index.ts:588`, `:1100` and `client/src/components/developers/QaWorkloadDevTable.vue:145` as `workloadAlert.consecutiveSprintCount` — an **unrelated QA-workload field**. Do NOT rename those. Only the bug-ratio-alerts payload changes. Likewise `startStage`/`endStage` appear in the cycle-time-boundaries **response** type (`types/index.ts:815-816,864-865,871-872`) and `WorkflowTab.vue:42-43,76-77` — the **GET response shape is NOT changing** (only the PUT request payload). Confirm whether the response also renames: the `SaveCycleTimeBoundariesResponse` record currently returns `(StartStage, EndStage)` — leaving the response keys as-is keeps the GET/response contract stable; only the PUT request changes. **Keep the response keys unchanged unless the team decides otherwise** (this avoids touching `WorkflowTab.vue` read paths).

**Accept:**
- `rg -n 'AlertThreshold|ConsecutiveSprintCount' src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/` shows the new `BugRatio*`-prefixed names only (zero bare `AlertThreshold`/`ConsecutiveSprintCount`).
- `rg -n 'public.*StartStage|public.*EndStage' src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/` shows `CycleTimeStartStage`/`CycleTimeEndStage` on the command (response record may keep `StartStage`/`EndStage`).
- `rg -n 'bugRatioAlertThreshold|bugRatioConsecutiveSprintCount|cycleTimeStartStage|cycleTimeEndStage' client/src/api/settings.ts` shows the new payload keys.
- `rg -n 'workloadAlert' client/src/components/developers/QaWorkloadDevTable.vue` still present (false-positive field NOT touched).
- `npm run build` (from `client/`) exits 0, no TypeScript errors.

### Step 7 — Apply Mapster to trivial-map handlers (ADR-010, incl. Save* settings per Q3)

**Disposition: None** (no Mapster-application skill — strongest future-skill candidate). **Confidence: medium** — the mechanism is simple; the judgment is *which* handlers qualify.

Replace hand-written object-to-object assignment with Mapster `.Adapt<T>()` + an `IRegister` map class beside each slice, **only where source and target are matching-shaped objects** (command↔entity or entity↔response with aligned field names). Place each `IRegister` implementation in the feature folder it serves (e.g. `Features/Settings/SaveBugRatioAlerts/`). For trivial 1:1 maps where Mapster's default convention already matches by name, an explicit `IRegister` may be unnecessary — but per Q4a `GlobalSettings.Scan` only discovers `IRegister` types, so any non-default mapping (e.g. updating an existing tracked entity in place) needs an `IRegister`. Use `.Adapt(existing)` (the two-arg form) to map onto the already-loaded entity rather than constructing a new one — the `Save*` handlers load `existing` then mutate it.

**Mapster-eligible (trivial, field-aligned after Step 6):**
- `Save*` settings command → `AppSettings` (in-place `.Adapt(existing)`): `SaveBugRatioAlerts`, `SaveBoard`, `SaveSyncConfig`, `SaveAnalyticsTargets`, `SaveHealthConfig` (the 5 config-object fields are reference-assigned — map them), `SaveBugRatioAlerts`/`SaveSyncConfig`/`SaveBoard`/`SaveAnalyticsTargets` (scalar settings).
- Response maps where entity→response is 1:1 by name: e.g. `UpdateSprintCommand` handler's entity→`UpdateSprintResponse` block (fields align: `Id`/`Name`/`StartDate`/`EndDate`/`Goal`; `State` is `sprint.State.ToString()` — that one member needs a custom map rule or post-adapt assignment); `UpdateTeamConfig` entity→response; `Identity UpdateRole`/`UpdateStatus` → `UserResponse` is assembled from TWO sources (User + Person) — **NOT a clean single-source Mapster case; leave hand-assembled** (or map the User part and hand-fill the Person part — developer's call, document it).

**NOT Mapster-eligible (do not force-fit — document why):**
- `SaveXraySettings` — conditional secret-preservation logic (`if (!string.IsNullOrEmpty(req.XrayClientSecret))`). Keep the handler logic; do not blanket-`.Adapt`.
- `SaveCycleTimeBoundaries` — stage-order validation + index lookup before assignment. Keep handler logic.
- `SaveDoneStatuses`/`SaveExcludedStatuses`/`SaveWorkflowStages` — list-replacement on the entity; trivial enough that a one-line assignment is fine, but if the field names align a `.Adapt` is acceptable. Developer's call; not a gate.
- `SyncXray`, `GetTeamRoster`, analytics queries — responses assembled from service results / multi-source aggregation, not a single matching-shaped object. Leave as-is.

This is the ADR-010 intent: trivial mapping → Mapster; non-trivial (conditional/multi-source/business logic) → stays explicit (Pass 3 may move non-trivial maps into aggregate factory methods).

**Accept:**
- `rg -n '\.Adapt(<|\()' src/Services/Fokus/Fokus.API/Features src/Services/Identity/Identity.API/Features | wc -l` ≥ 5 (Mapster is actually used, not just registered).
- At least one `IRegister` implementation exists beside a Settings slice (`rg -n ': IRegister' src/Services/Fokus/Fokus.API/Features/Settings/`).
- The four NOT-eligible handlers (`SaveXraySettings`, `SaveCycleTimeBoundaries`, plus the two-source `UserResponse` assembly) retain their conditional/multi-source logic — confirmed by reading, noted in implementation.md with the reason.
- `dotnet build SprintRituals.slnx` green.

### Step 8 — Verification sweep + build (both stacks)

**Disposition: None** (verification). **Confidence: high.**

Run the full grep-checkable acceptance battery and both builds. This is the objective done-gate.

**Backend:**
1. `rg -n 'class \w+Request\b|record \w+Request\b' --glob '*.cs' src/` → zero hits **except** intentionally-excluded non-contract types: `BadRequestException` (exception, not a request DTO) and the Refit interface name if any. Confirm each remaining hit is a sanctioned exclusion; expected: only `BadRequestException.cs`.
2. `rg -n 'RequestValidator' --glob '*.cs' src/` → zero hits.
3. `rg -n '^\s*(public\s+)?(class|record)\s+\w+(Request|Response|Validator|Dto)\b' --glob '*Endpoint.cs' src/` → zero hits (no type **defined** in endpoint files; usage lines like `EndpointWithoutRequest<MeResponse>` do not match). All 16 fat files split.
4. `rg --files src/Services -g '*Models.cs'` → nothing (filenames normalized).
5. `rg -n 'TeamRequest|SprintRequest|UpdateJiraSprintRequest' src/` → zero hits.
6. gRPC `[DataMember(Order=…)]` diff check (Step 5 accept).
7. `rg -n 'AddMapster' src/` → zero; `rg -n 'GlobalSettings.Scan' src/` → ≥ 2.
8. `dotnet build SprintRituals.slnx` → green (filter `error CS`; ignore MSB3026/3027/3492 file-lock per project-rules).

**Frontend:**
9. `npm run build` (from `client/`) → exit 0, no TS errors.
10. False-positive guard: `rg -n 'workloadAlert.consecutiveSprintCount' client/src` still present; cycle-time **response** read paths in `WorkflowTab.vue` unchanged (unless response rename was elected).

**Accept:** all 10 checks pass; both builds green; implementation.md records each grep's actual output.

## Cross-Service Changes

**gRPC (`Blocks.Contracts`):** `TeamRequest`→`TeamQuery`, `SprintRequest`→`SprintQuery` (Step 5). **Wire-compatible** — protobuf-net.Grpc serializes on `[DataMember(Order)]`, which is preserved; the C# type-name change does not alter the protobuf message. No gRPC client exists yet (Reflekt port is future), so server impls + the two `[ServiceContract]` interfaces are the only consumers. No proto regeneration (code-first — no `.proto` files).

**Jira module (`Jira.Contracts`):** `UpdateJiraSprintRequest`→`UpdateJiraSprintCommand` (Step 5). Refit serializes by property name; type rename is wire-safe. Consumers: `IJiraApi.cs`, `RestApiJiraClient.cs`.

**Frontend (`client/`):** Path-A field renames change two PUT request payloads (Step 6). Hand-written TS — no codegen. Enumerated blast radius: `api/settings.ts`, `stores/settingsStore.ts`, and the Settings-tab call sites. GET/response contracts unchanged (unless response rename elected).

## Migration Notes

No EF migrations (no schema change). The only "migration" is the package addition (Step 1) which `dotnet build` restores. No seed-data change.

## Testing Strategy

No automated suite (project decision — manual testing after each pass per `docs/backlog.md`). Verification is:
1. **Grep-checkable acceptance** per step (Step 8 battery) — objective, the primary gate.
2. **Backend build** — `dotnet build SprintRituals.slnx` green after each code step.
3. **Frontend build** — `npm run build` exit 0 after Step 6.
4. **Manual smoke (user, post-pass):** save each Settings tab (bug-ratio-alerts + cycle-time-boundaries especially — the two with field renames) and confirm the values persist; exercise the Update Sprint and Update Team Config flows (Mapster response maps); confirm gRPC server still starts (h2c endpoint) and Jira sprint update still works (Jira contract rename).

## KB Impact

None expected — Pass 2 changes type names, file layout, and mapping mechanism, not business rules, formulas, or edge cases. `docs/kb/` is empty (verified Pass 0). No KB entry to update; no numbered KB step warranted.

## Open Questions

None blocking — Q1–Q5 are Answered (questions.md). One developer-time judgment is flagged inline, not a blocker:
- **Step 6 response-rename choice:** whether to also rename the `SaveCycleTimeBoundariesResponse` keys (`StartStage`/`EndStage`). Plan recommends **leaving the response keys unchanged** to keep the GET/read contract and `WorkflowTab.vue` read paths stable — only the PUT request payload changes. If the developer finds a reason the response must match, that's a flag-to-architect, not a silent change.

**Flags for the team-lead (not blockers):**
1. **Mapster version** — Step 1 uses "latest stable" per `framework-currency`; pin the exact version at implementation time and record it.
2. **Mapster-application skill gap** — Steps 6/7 have no skill. Strongest candidate for a new `apply-mapster-mapping` skill (ADR-010). Logged to lessons for the learner.
3. **gRPC naming convention** — gRPC has no Command/Query idiom; the plan renames `*Request`→`*Query` for consistency with the read-shaped operations. If the team prefers a gRPC-specific suffix, flag before Step 5.
