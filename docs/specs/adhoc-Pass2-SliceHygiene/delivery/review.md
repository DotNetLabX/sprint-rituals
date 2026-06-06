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
