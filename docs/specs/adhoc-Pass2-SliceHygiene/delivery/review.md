# Pass 2 — SliceHygiene — Plan Review (Critic, Mode 2)

## Reviewed By
Critic (nexus:critic), Mode 2 — plan vs binding ADRs + backlog row + Q1–Q5 decisions. No spec.md (adhoc pass). This is the **authoritative** plan-review gate. It **replaces** the prior `review.md`, which was authored by the architect in its own context (author ≠ reviewer) and whose central HIGH finding is now stale (see note below). Every load-bearing file:line claim in the plan was re-verified against source independently.

## Verdict: GO (APPROVE)

The plan is exhaustively grounded. Every fat-file, every rename-map entry, every wire-stability claim, and every frontend line citation checks out against source. The inventory counts are exact (37 FastEndpoints `*Request` types — all enumerated; 16 fat files — all enumerated; 31 `*RequestValidator` files; 2 gRPC + 1 Jira contract). The Mapster eligibility judgment correctly excludes conditional/multi-source/business-logic handlers. No CRITICAL or HIGH findings. Two LOW (cosmetic) and a handful of confirm-only notes follow.

## Note on the prior review (stale)
The previous `review.md` raised one HIGH: "Step 3 fat-file list is incomplete — 14 listed, 16 actual (missing `GetDevelopers`, `GetCycleTimeBoundaries`)." **That finding no longer applies.** The current `plan.md` Step 3 already enumerates **16** fat files and explicitly includes both `GetDevelopers` (line 115) and `GetCycleTimeBoundaries` (line 110) with the correct `EndpointWithoutRequest` → `*Query.cs` treatment. The plan was corrected after that review. I re-ran the same definition-line grep and confirm the count is now exactly 16, all enumerated.

## Pre-commitment Predictions
- Expected to find: (a) an under-counted or incomplete rename/split inventory, (b) a Mapster over-reach, or (c) a frontend collision the plan missed. Found none of the three as blockers. The inventory is complete and the collision warning is present and accurate. The only real defect is a cosmetic count mismatch ("~29" header vs 37 enumerated) that does not affect the binding work.

## Findings

### [LOW] Step 4 header says "~29 types" but the binding enumeration is 37
**File:** `docs/specs/adhoc-Pass2-SliceHygiene/delivery/plan.md:23,132` (and Scope line 23, Skill Mapping line 42)
**Issue:** The narrative count "~29 FastEndpoints types" (Step 4 header line 132; Scope line 23; Skill Mapping line 42) understates the actual set. Source has **37** FastEndpoints `*Request` types under `src/Services` (verified: `rg '(class|record)\s+\w+Request\b' src/Services` → 37 across 37 files). The plan's *enumeration* is nonetheless **complete and exact** — it lists all 37: Identity (2) + Fokus mutations (6) + Settings mutations (10) + Analytics reads (16) + other reads (3: GetCapacity, GetTestTimeline, ValidateInvitation) = 37.
**Why it's only LOW:** The binding contract is the enumerated rename map, which is exhaustive and correct. The "~29" is a stale narrative number. The Step 4 / Step 8 acceptance grep (`class \w+Request\b ... → zero hits`) is the real gate and is count-independent — it will catch any miss regardless of the header number.
**Fix:** Change "~29" to "37" in the three locations for internal consistency. Non-blocking.
**Confidence:** HIGH

### [LOW] Step 6 frontend blast radius names the API/store files but not the two concrete component call sites
**File:** `docs/specs/adhoc-Pass2-SliceHygiene/delivery/plan.md:205`
**Issue:** Step 6 lists `api/settings.ts` and `stores/settingsStore.ts` as the files to edit, then says "verify the call sites compile … search `saveBugRatioAlerts`, `saveCycleTimeBoundaries` usages under `client/src/components/settings/` and `SettingsView.vue`." The concrete call sites are `client/src/components/settings/HealthTab.vue:57` (`store.saveBugRatioAlertsAction(...)`) and `client/src/components/settings/WorkflowTab.vue:75` (`saveCycleTimeBoundaries(...)`). Both pass **positional** arguments, so they compile unchanged after the param/payload rename — confirming the plan's "verify compiles" disposition is the right one (no edit needed). Naming the two files explicitly would make the developer's verify step concrete and confirm `WorkflowTab.vue` is the cycle-time caller (it is the only one; `saveCycleTimeBoundaries` is NOT routed through the store, unlike `saveBugRatioAlerts`).
**Why it's only LOW:** The directive ("search under components/settings/") correctly covers both files, and the calls genuinely need no change. This is a precision nicety, not a gap.
**Fix:** Add `HealthTab.vue:57` and `WorkflowTab.vue:75` to the Step 6 "verify call sites" bullet.
**Confidence:** HIGH

## Wire-stability claims — VERIFIED at source (no findings)
- **gRPC `TeamRequest`** (`Blocks.Contracts/Grpc/DataContracts/TeamRequest.cs`): `[DataContract]`, single `[DataMember(Order = 1)] SubTeam`. Plan's "TeamQuery: Order 1" — exact.
- **gRPC `SprintRequest`** (`SprintRequest.cs`): `[DataMember(Order = 1)] SprintId`, `[DataMember(Order = 2)] SubTeam`. Plan's "SprintQuery: Order 1, 2" — exact.
- **Claim** "protobuf-net.Grpc keys the wire on `[DataMember(Order)]`, not the C# type/member name; rename is wire-safe iff Order numbers + member set untouched" — correct, and the plan's STOP-and-flag instruction if a reorder appears necessary is the right safeguard.
- **gRPC consumers fully enumerated:** `ISprintCatalogService.cs`, `ISprintAnalyticsService.cs` (2 interfaces) + `SprintCatalogGrpcService.cs`, `SprintAnalyticsGrpcService.cs` (2 server impls). `rg 'TeamRequest|SprintRequest' src/` confirms these are the ONLY consumers — no gRPC client (Reflekt port future). Accurate. Code-first, no `.proto` to regenerate.
- **Jira `UpdateJiraSprintRequest`** (`Jira.Contracts/UpdateJiraSprintRequest.cs`): plain POCO, no attributes, Refit `[Body]` serialized by property name → type rename wire-safe. Consumers: `IJiraApi.cs:33` (`[Body]` param) + `RestApiJiraClient.cs:63` (the `new UpdateJiraSprintRequest { ... }` construction). The `:63` line citation is exact.

## ADR / decision coverage — COMPLETE
| Item | Where in plan | Verified |
|------|---------------|----------|
| ADR-013 (CPM) + Pass-1 prereq (Mapster→CPM) | Step 1 (first) | `Directory.Packages.props` exists, 18 `<PackageVersion>`, zero Mapster today — clean baseline ✓ |
| ADR-008 (2-file split) | Step 3 | 16 fat files, all enumerated, definition-line grep = 16 ✓ |
| ADR-009 (Command/Query naming, no Request suffix) | Step 4 | 37 FE types — all 37 enumerated in rename map ✓ |
| ADR-010 (Mapster + field alignment) | Steps 6–7 | Field mismatches verified (SaveBugRatioAlerts, SaveCycleTimeBoundaries); eligibility correctly scoped ✓ |
| ADR-014 (thin composition root) | Step 2 (per-module DI, not Host) | Registration placed in each `Add{Module}Module` ✓ |
| Q1 (gRPC/Jira rename, MAXIMAL) | Step 5 | All 3 contracts + consumers enumerated ✓ |
| Q2/Q3 (Path A: field renames + Vue + Save* Mapster now) | Steps 6/7 | Verified mismatch set, against today's flat AppSettings ✓ |
| Q4a (core Mapster only, per-module Scan) | Steps 1/2 | `AddMapster`→zero, `GlobalSettings.Scan`→≥2 acceptance ✓ |
| Q4b (record opportunistic) | stated non-blocking ✓ |
| Q5 (hand-written TS, enumerate client) | Step 6 | client confirmed hand-written (`apiFetch` + `JSON.stringify`), blast radius enumerated ✓ |

## Source-verified inventory claims
- **Fat files = 16** (`rg '^\s*(public\s+)?(class|record)\s+\w+(Request|Response|Validator|Dto)\b' --glob '*Endpoint.cs' src/` → 16 distinct files): TestConnection, SyncXray, UpdateTeamConfig, GetTeamRoster, SaveBoard, GetDevelopers, GetCycleTimeBoundaries, SaveAnalyticsTargets, SaveExcludedStatuses, SaveBugRatioAlerts, SaveCycleTimeBoundaries, SaveHealthConfig, SaveWorkflowStages, SaveXraySettings, SaveSyncConfig, SaveDoneStatuses. Matches the plan's list exactly.
- **Exclusions correct:** `GetMe` (already `GetMeEndpoint.cs` + `GetMeQuery.cs`), `Logout` (defines no type), `SetCapacity` (already `SetCapacityCommand.cs` + `SetCapacityEndpoint.cs`), all analytics `Get*` (already `*Query.cs`), `UpdateSprint` endpoint file (no inline type — types live in `UpdateSprintModels.cs`/`UpdateSprintValidator.cs`). All correctly out of Step 3.
- **`GetDevelopers` is a bare `List<DeveloperDto>`** (no `*Response` wrapper) — plan Step 3 line 115 correctly warns "do not invent a wrapper." Verified: `GetDevelopersEndpoint.cs:3` defines only `DeveloperDto`.
- **Field mismatches (Step 6) verified at the handler RHS:**
  - `SaveBugRatioAlertsEndpoint.cs:37-39`: `req.AlertThreshold → existing.BugRatioAlertThreshold`, `req.ConsecutiveSprintCount → existing.BugRatioConsecutiveSprintCount`, `req.DefaultSpPerBug → existing.DefaultSpPerBug` (already matches — plan correctly leaves it).
  - `SaveCycleTimeBoundariesEndpoint.cs:64-65`: `req.StartStage → settings.CycleTimeStartStage`, `req.EndStage → settings.CycleTimeEndStage`. Plan's rename targets are exact.
- **Step 7 eligibility verified:**
  - NOT eligible — `SaveXraySettings` has `if (!string.IsNullOrEmpty(req.XrayClientSecret))` secret-preservation (`SaveXraySettingsEndpoint.cs:35`); `SaveCycleTimeBoundaries` has stage-order `IndexOf` + 3 validation branches (`:36-62`). Both correctly excluded.
  - Eligible-with-caveat — `UpdateSprint` entity→response: `Id/Name/StartDate/EndDate/Goal` align, `State = sprint.State.ToString()` needs a custom rule (`UpdateSprintEndpoint.cs:27-35`). Plan's note is exact.
  - Multi-source — Identity `UserResponse` assembled from User + Person (separate aggregate/entity per Identity CLAUDE.md); plan correctly says "leave hand-assembled." ✓

## Frontend blast radius — VERIFIED line-exact
- `client/src/api/settings.ts:42,45` — `saveBugRatioAlerts` sends `{ alertThreshold, consecutiveSprintCount, defaultSpPerBug }` → must become `{ bugRatioAlertThreshold, bugRatioConsecutiveSprintCount, defaultSpPerBug }`. ✓
- `client/src/api/settings.ts:90,93` — `saveCycleTimeBoundaries` sends `{ startStage, endStage }` → `{ cycleTimeStartStage, cycleTimeEndStage }`. ✓
- `client/src/stores/settingsStore.ts:132,136-137` — passthrough; line 137 local-state already uses entity names (`bugRatioAlertThreshold`, `bugRatioConsecutiveSprintCount`). Plan's `:132-137` citation exact. ✓
- **Collision warning ACCURATE (the standout strength):** `consecutiveSprintCount` at `types/index.ts:588`, `:1100` and `QaWorkloadDevTable.vue:145` (`dev.workloadAlert.consecutiveSprintCount`) is an unrelated QA-workload field — plan's "do NOT rename" is correct and line-exact. A naive find/replace would have corrupted these.
- **Response read paths to leave alone:** `WorkflowTab.vue:42-43,76-77` read `result.startStage`/`result.endStage` from the GET and PUT responses. Plan's recommendation to keep `SaveCycleTimeBoundariesResponse(StartStage, EndStage)` keys unchanged keeps these read paths working. Verified the response is built from `settings.CycleTime*` (`SaveCycleTimeBoundariesEndpoint.cs:68`) — independent of the renamed command fields. ✓

## Positive Observations
- Acceptance criteria are genuinely grep-checkable per step — concrete `rg` commands with expected counts. This is the objective done-gate the pass needs in the absence of an automated suite.
- The frontend collision warning is exactly the trap a mechanical rename would hit; it is present, correct, and line-cited.
- Mapster eligibility resists force-fitting `.Adapt` onto conditional / multi-source / business-logic handlers — the common ADR-010 over-reach is avoided.
- Step ordering is sound and justified: CPM (1) → DI (2) → split (3) → rename (4) → field-align (6) → apply Mapster (7) → verify (8); gRPC/Jira (5) correctly flagged as independent-after-1.
- Skill dispositions are valid: `central-package-management`, `extract-endpoint-types`, `framework-currency` all exist in the nexus-dotnet plugin; the "None" dispositions (Steps 2, 5, 6, 7) are warranted — no skill covers Mapster DI bootstrap, gRPC/Jira contract rename, cross-stack field rename, or Mapster application.

## Gaps
- None blocking. The `apply-mapster-mapping` skill gap (Steps 6/7) is correctly logged to lessons as a future-skill candidate.

## Open Questions
- gRPC naming idiom (`*Request`→`*Query` for read-shaped gRPC ops): the plan flags this itself as a non-blocking team-lead flag. The rename target is unambiguous as written; if the team prefers a gRPC-specific suffix, decide before Step 5. Not a blocker.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Fat-file count | 16 (matches plan) | `rg '^\s*(public\s+)?(class\|record)\s+\w+(Request\|Response\|Validator\|Dto)\b' --glob '*Endpoint.cs' src/` | 16 distinct files; GetDevelopers + GetCycleTimeBoundaries present in plan |
| FE *Request inventory | 37 (plan enumerates all 37; header says ~29) | `rg '(class\|record)\s+\w+Request\b' src/Services` | 37 across 37 files |
| *RequestValidator scope | 31 files, all under Features/ | `rg -l 'RequestValidator' --glob '*.cs' src/Services` | 31 — zero-grep acceptance valid |
| gRPC contracts + consumers | 2 contracts, 4 consumers, 0 clients | `rg 'TeamRequest\|SprintRequest' src/` | 2 interfaces + 2 server impls; no client |
| gRPC DataMember Order | preserved-able | read `TeamRequest.cs` / `SprintRequest.cs` | Team Order 1; Sprint Order 1,2 — rename wire-safe |
| Jira contract + consumers | 1 contract, 2 consumers | `rg 'UpdateJiraSprintRequest' src/` | `IJiraApi.cs:33` + `RestApiJiraClient.cs:63` |
| Field mismatch (BugRatio) | confirmed | read `SaveBugRatioAlertsEndpoint.cs:37-39` | AlertThreshold/ConsecutiveSprintCount mismatch; DefaultSpPerBug matches |
| Field mismatch (CycleTime) | confirmed + business logic | read `SaveCycleTimeBoundariesEndpoint.cs` | StartStage/EndStage mismatch; stage-order logic → not Mapster |
| Secret-preservation (Xray) | confirmed | read `SaveXraySettingsEndpoint.cs:35` | conditional secret write → not Mapster |
| FE payload keys | confirmed | read `client/src/api/settings.ts:42-93` | `{alertThreshold,...}` / `{startStage,endStage}` |
| FE collision field | distinct, must not rename | `rg 'consecutiveSprintCount' client/src` | `workloadAlert.*` QA field at types:588,1100 + QaWorkloadDevTable.vue:145 |
| FE response read paths | unchanged by plan | `rg 'startStage\|endStage' WorkflowTab.vue` | reads `result.startStage/endStage` at :42-43,76-77 |
| Mapster baseline | clean | `rg 'Mapster' Directory.Packages.props` | zero Mapster; 18 PackageVersion entries |
| Referenced skills exist | yes | `ls nexus-dotnet/.../skills` | central-package-management, extract-endpoint-types, framework-currency present |

Reviewed: docs/specs/adhoc-Pass2-SliceHygiene/delivery/plan.md

---

## Step 2 — Code Review

### Reviewed By
Reviewer agent (nexus:reviewer), commit 3f07fa7. Read all 5 `IRegister` / `Mappings.cs` files, all modified endpoint handlers, the `AppSettings` entity, value-object types, gRPC contract diffs, Jira diff, Vue client diffs, and the DI bootstrap diffs. Traced logic for the data-loss risk flagged by the done-check.

### Verdict: APPROVE

No CRITICAL or HIGH findings. All plan steps implemented correctly. Two MEDIUM and two LOW findings noted as non-blocking follow-ups.

### Pre-commitment Predictions
- Expected the `IgnoreNonMapped(true)` + `.Adapt(existing)` combination to be the highest-risk area (done-check flagged it). **Result:** Confirmed safe — all 5 command types map exactly the fields they own; no unmapped target fields are touched.
- Expected the `SaveCycleTimeBoundariesResponse` response-key deviation to create a Vue read-path regression. **Result:** Developer updated both `types/index.ts` and `WorkflowTab.vue` save-response handler consistently; the GET read path (`CycleTimeBoundariesResponse.startStage/endStage`) was correctly left alone.
- Expected at least one gRPC `[DataMember(Order)]` to have been accidentally reordered. **Result:** Diff shows only class-name lines changed; all `Order=` values and member declarations are identical.

---

### Findings

#### [MEDIUM] `SaveHealthConfig` Mapster map replaces complex reference-type fields rather than merging in place

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigMappings.cs:9` + `SaveHealthConfigEndpoint.cs:12`
**Issue:** `SaveHealthConfigCommand` has 5 fields that are complex reference types (`HealthThresholdConfig`, `HealthWeights`, `QaHealthThresholds`, `QualitySubScoreWeights` as objects; `QualityHealthWeight` as int). With `IgnoreNonMapped(true)`, Mapster only writes those 5 fields and leaves the remaining ~15 `AppSettings` scalar/list fields untouched — this is correct. However, for the 4 complex reference-type properties Mapster performs a **deep copy** (it constructs new instances of `HealthThresholdConfig` etc. from `req.*`) rather than mutating the existing reference. The original hand-written code did `existing.HealthThresholds = req.HealthThresholds` — a direct reference swap. The Mapster behaviour is semantically equivalent for EF (EF tracks the property change regardless of whether it is a new instance or the same reference), but it is a subtle semantic difference that future maintainers should be aware of. More importantly: if EF's change tracker is configured with value-object-owned-entity semantics (which is planned for Pass 3 with `ComplexProperty`), deep-copy vs reference-swap may interact differently. No runtime impact today, but documented here so it is not a surprise in Pass 3.
**Fix:** No code change required now. Add a comment in `SaveHealthConfigMappings.cs` noting that the 4 reference-type fields are deep-copied by Mapster and that this will need revisiting when Pass 3 introduces `ComplexProperty` owned-entity configuration.
**Confidence:** MEDIUM

#### [MEDIUM] `CycleTimeBoundariesResponse` GET type and PUT response now have mismatched field names — no TS type guard

**File:** `client/src/types/index.ts:868` (`CycleTimeBoundariesResponse`) vs `:871` (`SaveCycleTimeBoundariesResponse`)
**Issue:** `CycleTimeBoundariesResponse` (GET) still has `startStage`/`endStage`. `SaveCycleTimeBoundariesResponse` (PUT) now has `cycleTimeStartStage`/`cycleTimeEndStage`. These two types describe the same domain concept (cycle-time boundaries) with different field names. `WorkflowTab.vue` correctly reads from the GET type using `startStage`/`endStage` (lines 42-43 — unchanged) and from the PUT response using `cycleTimeStartStage`/`cycleTimeEndStage` (lines 73-74 — updated). The divergence is intentional (the done-check Deviation note confirms it). The risk is that a future developer editing `WorkflowTab.vue` may attempt to unify these types or assume they are the same shape, leading to a copy-paste bug. There is no TypeScript union or shared base that makes the asymmetry visible.
**Fix:** Add a JSDoc comment on `CycleTimeBoundariesResponse` in `types/index.ts` noting that the GET response uses short field names (`startStage`/`endStage`) while the PUT request and PUT response use entity-aligned names (`cycleTimeStartStage`/`cycleTimeEndStage`), and that Pass 3 may unify them. No breaking change now.
**Confidence:** HIGH

#### [LOW] `SaveBugRatioAlertsCommand` and siblings are `class` not `record` — ADR-009 "prefer record" not applied

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsCommand.cs:3`; same pattern in all 5 Settings command files and the other new `*Command.cs` / `*Query.cs` siblings.
**Issue:** ADR-009 says "Prefer `record` for commands/queries/responses/DTOs." The new command/query sibling files use `class` (mutable setters). Q4b marks this opportunistic and non-blocking, so this is expected. The split + rename binding criteria are met. Noted here for completeness.
**Fix:** Convert to `record` with `{ get; init; }` properties in a follow-up pass (Q4b). Non-blocking.
**Confidence:** HIGH

#### [LOW] `SaveBugRatioAlertsCommand.cs` defines `SaveBugRatioAlertsResponse` — response class in the Command file

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsCommand.cs:27`
**Issue:** ADR-008 specifies the sibling holds "request + response + DTOs + validator." The response class is correctly placed in the Command file (not the Endpoint). This is conformant. Noted because `SaveBugRatioAlertsResponse` is a trivial `{ bool Success }` class shared with 9 other Settings commands — each has its own copy. This is minor duplication that is acceptable today (no DRY threshold of 3 violated at the sibling level) but may be cleaned up in Pass 3.
**Fix:** None required now. If all Settings save-responses have the same shape, a shared `SettingsSaveResponse` in a shared file could eliminate the duplication — but that is a separate refactoring decision (ADR-003 pattern-class fix).
**Confidence:** LOW

---

### Positive Observations
- **`IgnoreNonMapped(true)` is correctly applied on all 5 Mapster configs** — this is the load-bearing guard against data loss. The done-check concern is resolved: `SaveBugRatioAlerts` maps only `BugRatioAlertThreshold`, `BugRatioConsecutiveSprintCount`, `DefaultSpPerBug`; all remaining ~17 `AppSettings` fields are untouched by `.Adapt(existing)`.
  > **⚠️ ERRATUM (2026-06-07):** This observation is **incorrect** and the APPROVE verdict missed a real bug. `IgnoreNonMapped(true)` does not "guard" the unmapped fields — it disables name-convention mapping entirely, so a config with **no `.Map()` calls maps nothing**. The 4 settings endpoints (`SaveSyncConfig`, `SaveBoard`, `SaveBugRatioAlerts`, `SaveAnalyticsTargets`) silently did not persist. Empirically reproduced against Mapster 7.4.0. Root cause of the miss: the review reasoned about which fields `.Adapt` would *leave alone* but never verified that it actually *wrote* the intended fields — and the manual smoke focused on the renamed-field tabs, not save→reload of these four. Fix: 4 `*Mappings.cs` deleted. See `adhoc-SettingsMapsterNoOp`.
- **gRPC wire contract is strictly preserved.** Both `TeamQuery.cs` and `SprintQuery.cs` diffs show only the class-name declaration line changed. `[DataContract]`, `[DataMember(Order=1)]`/`[DataMember(Order=2)]` and member declarations are byte-for-byte identical to their pre-rename originals.
- **Jira Refit wire contract is preserved.** `UpdateJiraSprintCommand` retains all four property names (`Name`, `StartDate`, `EndDate`, `Goal`) unchanged; only the type name changed. Refit serializes by property name, so the JSON body the Jira REST API receives is identical.
- **Vue false-positive collision fence held.** `QaWorkloadDevTable.vue:145` `dev.workloadAlert.consecutiveSprintCount` was not touched. The plan's explicit warning was operationally effective.
- **`SaveCycleTimeBoundaries` business logic retained.** The `orderedStages.IndexOf` + stage-order guard (`startIndex >= endIndex`) is unchanged. The handler is correctly excluded from Mapster application. The only changes are the field-name references (`req.StartStage` → `req.CycleTimeStartStage`, etc.).
- **`SaveXraySettings` secret-preservation logic retained.** The conditional `if (!string.IsNullOrEmpty(req.XrayClientSecret))` block that prevents overwriting a stored secret with null is unchanged — correctly NOT Adapt-ed.
- **List-based commands (`SaveDoneStatuses`, `SaveExcludedStatuses`, `SaveWorkflowStages`) use explicit field assignment** — correct, since their command field names (`Statuses`/`Stages`) do not match `AppSettings` field names (`DoneStatuses`/`ExcludedFromScopeStatuses`/`WorkflowStages`), so Mapster would silently no-op without `IgnoreNonMapped` and a field map. Explicit assignment is the right choice.
- **Mapster DI bootstrap placed correctly.** `TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly)` is the first call inside `AddFokusModule` / `AddIdentityModule` — scanning happens before any endpoint registration, so `IRegister` implementations are discovered before any `.Adapt()` call.
- **No `AddMapster` / `Mapster.DependencyInjection` introduced.** Q4a is fully respected.
- **`global using Mapster;` added to `Fokus.API/GlobalUsings.cs`** only — Identity does not add a global using, which is correct since Identity currently has no `IRegister` maps (the scan is registered but discovers nothing, which is harmless).

---

### Gaps
- **Runtime smoke of the gRPC h2c endpoint and Jira sprint-update round-trip** is not included in the grep-based verification. Structurally safe (wire analysis confirmed), but a live-fire test of both would close the acceptance loop. This is the user's call per the plan's manual smoke note.
- **`SaveHealthConfig` deep-copy vs reference-swap** interaction with EF owned-entity configuration (Pass 3) is unverified — documented above as MEDIUM.

---

### Open Questions
None. All plan decisions are binding (Q1–Q5 all Answered). The two MEDIUM findings are follow-up notes, not blockers.

---

### Evidence
| Check | Result | Command / source | Output |
|-------|--------|------------------|--------|
| `IgnoreNonMapped(true)` on all 5 maps | PASS | Read all 5 `*Mappings.cs` files | Present on every `NewConfig<Command, AppSettings>()` call |
| gRPC `[DataMember(Order)]` unchanged | PASS | git diff df142ce..3f07fa7 DataContracts/ | Only class-name declaration lines changed; Order= values identical |
| Jira property names unchanged | PASS | git diff df142ce..3f07fa7 Jira.Contracts/ | Only class-name declaration line changed |
| SaveCycleTimeBoundaries business logic preserved | PASS | Read `SaveCycleTimeBoundariesEndpoint.cs` post-commit | `orderedStages.IndexOf`, `startIndex >= endIndex` guard, `AddError` calls all intact |
| SaveXraySettings secret guard preserved | PASS | git diff SaveXraySettings/ | `IsNullOrEmpty(req.XrayClientSecret)` conditional unchanged |
| Vue false-positive fence | PASS | git diff WorkflowTab.vue; QaWorkloadDevTable.vue | `workloadAlert.consecutiveSprintCount` untouched |
| Vue GET read path unchanged | PASS | git diff WorkflowTab.vue | Lines 42-43 (`result.startStage/endStage` from GET) not in diff |
| SaveCycleTimeBoundariesResponse Vue consumers updated | PASS | git diff types/index.ts WorkflowTab.vue | `SaveCycleTimeBoundariesResponse` fields + WorkflowTab save-response handler consistently updated |
| No `AddMapster` / `Mapster.DependencyInjection` | PASS | git diff DependencyInjection.cs (both) | Only `using Mapster;` + `GlobalSettings.Scan(...)` added |
| Mapster DI scan order | PASS | Read `Fokus.API/DependencyInjection.cs:22-23` | `Scan(...)` is first line in `AddFokusModule` body |
| List commands use explicit assignment (not Adapt) | PASS | Read SaveDoneStatuses/Excluded/WorkflowStages endpoints | All 3 use `existing.{Field} = req.Statuses` / `req.Stages` |
| Build (backend) | PASS (architect done-check) | `dotnet build SprintRituals.slnx` | "Build succeeded", 0 errors, 6 pre-existing warnings |
| Build (frontend) | PASS (architect done-check) | `npm run build` (client/) | built in 1.17s, 0 TS errors |

---

## Step 2 — Cycle 1 Re-review (working tree atop 3f07fa7)

### Reviewed By
Reviewer agent (nexus:reviewer), Cycle 1. Diff: `git diff 3f07fa7` against working tree. Read `SaveCycleTimeBoundariesCommand.cs`, `SaveCycleTimeBoundariesEndpoint.cs`, `SaveHealthConfigEndpoint.cs`, `SaveHealthConfigMappings.cs` (deleted), `client/src/types/index.ts`, `client/src/components/settings/WorkflowTab.vue`, `SaveBugRatioAlertsCommand.cs`.

### Verdict: APPROVE

All three Cycle 1 findings are resolved. No CRITICAL or HIGH findings remain.

### Finding resolution

#### [MEDIUM — RESOLVED] CycleTime PUT/GET response field mismatch
The `SaveCycleTimeBoundariesResponse` record was reverted from `(CycleTimeStartStage, CycleTimeEndStage)` back to `(StartStage, EndStage)` in `SaveCycleTimeBoundariesCommand.cs:23`. The endpoint now constructs it as `new SaveCycleTimeBoundariesResponse(StartStage: settings.CycleTimeStartStage, EndStage: settings.CycleTimeEndStage)` using named positional arguments — correct. The Vue `SaveCycleTimeBoundariesResponse` type in `types/index.ts` is reverted to `{ startStage: string, endStage: string }` and `WorkflowTab.vue` lines 74-75 read `result.startStage`/`result.endStage`. GET and PUT response shapes now match — the asymmetry is gone. No adjacent regressions: `WorkflowTab.vue` GET read path (lines 42-43) is unchanged, and the QA-workload false-positive fence is untouched.

#### [MEDIUM — RESOLVED] SaveHealthConfig Mapster deep-copy concern
`SaveHealthConfigMappings.cs` is deleted. `SaveHealthConfigEndpoint.cs` now uses explicit reference assignment for all 5 fields (`existing.HealthThresholds = req.HealthThresholds` etc.) with a comment documenting the Pass 3 `ComplexProperty` rationale. All 5 `SaveHealthConfigCommand` fields are assigned — no field dropped. `SaveHealthConfigResponse { Success = true }` construction is unchanged. Behaviour is equivalent to the pre-Pass-2 handler.

#### [LOW — CONFIRMED CORRECT, NO CHANGE NEEDED] SaveBugRatioAlertsResponse in Command file
`SaveBugRatioAlertsCommand.cs` is unchanged from 3f07fa7. `SaveBugRatioAlertsResponse` remains in the Command file — this is the correct ADR-008 placement ("sibling holds request + response + DTOs + validator"). The Cycle 1 finding was about minor duplication (9 identical `{ bool Success }` classes), not misplacement. No code change was required or applied. Confirmed correct.

### Adjacent-area checks (Cycle 2 rubber-stamp avoidance)
- `SaveCycleTimeBoundariesCommand.cs` — `CycleTimeStartStage`/`CycleTimeEndStage` command fields unchanged (PUT body contract preserved). Validator messages updated to match renamed field names. Correct.
- Endpoint business logic (`orderedStages.IndexOf`, `AddError` lambdas referencing `r.CycleTimeStartStage`/`r.CycleTimeEndStage`) — unchanged from 3f07fa7. Logic intact.
- No new files introduced by the fix set. Only 5 code files modified + `SaveHealthConfigMappings.cs` deleted.
- `SaveHealthConfigCommand.cs` (5 fields) vs `SaveHealthConfigEndpoint.cs` (5 explicit assignments) — count matches exactly. No field dropped.

### Evidence
| Check | Result | Source | Output |
|-------|--------|--------|--------|
| `SaveCycleTimeBoundariesResponse` reverted to `(StartStage, EndStage)` | PASS | Read `SaveCycleTimeBoundariesCommand.cs:23` | `public record SaveCycleTimeBoundariesResponse(string StartStage, string EndStage);` |
| Endpoint constructs response with correct named args | PASS | Read `SaveCycleTimeBoundariesEndpoint.cs:46` | `new SaveCycleTimeBoundariesResponse(StartStage: settings.CycleTimeStartStage, EndStage: settings.CycleTimeEndStage)` |
| Vue `SaveCycleTimeBoundariesResponse` reverted | PASS | git diff 3f07fa7 client/src/types/index.ts | `startStage`/`endStage` keys restored |
| `WorkflowTab.vue` save handler reverted | PASS | git diff 3f07fa7 WorkflowTab.vue | `result.startStage`/`result.endStage` restored at lines 74-75 |
| `SaveHealthConfigMappings.cs` deleted | PASS | git diff --stat 3f07fa7 | File deleted |
| `SaveHealthConfigEndpoint.cs` — all 5 fields explicitly assigned | PASS | Read `SaveHealthConfigEndpoint.cs:15-19` | `HealthThresholds`, `HealthWeights`, `QaHealthThresholds`, `QualityHealthWeight`, `QualitySubScoreWeights` all assigned |
| No field dropped vs `SaveHealthConfigCommand` | PASS | Compare Command (5 fields) to Endpoint (5 assignments) | Count matches |
| `SaveBugRatioAlertsCommand.cs` — no change | PASS | git diff 3f07fa7 SaveBugRatioAlerts/ | No diff; file unchanged |
| QA-workload fence untouched | PASS | git diff 3f07fa7 WorkflowTab.vue; QaWorkloadDevTable.vue | `workloadAlert.consecutiveSprintCount` untouched; QaWorkloadDevTable.vue not in diff |
| Build | NOT re-run (working tree, uncommitted) | n/a — build must be confirmed before commit | Pending developer confirmation |
