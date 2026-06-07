# Pass 2 — SliceHygiene — Step 1 Done Check

**Checker:** Architect (Step 1 done check — conformance, not code review).
**Verdict: PASS** — all 8 plan steps Implemented or Deviated-with-reason. No Missing steps. Both builds green from independent fresh runs.
**Commit under check:** 3f07fa7 (developer self-commit).

## Method

Re-ran every grep-accept from `plan.md` independently against the real `src/` and `client/` files — did NOT trust `implementation.md`'s claimed outputs. Ran both builds fresh.

## Pre-commitment predictions (made before reading implementation.md)
1. Step 5 (gRPC/Jira rename) most likely partial — esp. the `RestApiJiraClient.cs:63` construction site. → **Held: fully done**, `:63` updated.
2. Step 6 frontend false-positive fence (`workloadAlert.consecutiveSprintCount`) most likely broken by find/replace. → **Held: fence intact**, unrelated QA field untouched.
3. Step 7 Mapster either unused (registered-not-called) or force-fit onto excluded handlers. → **Held: 5 real `.Adapt` calls; excluded handlers retain their logic.**

## Per-step disposition

| Step | What | Disposition | Independent evidence |
|------|------|-------------|----------------------|
| 1 | Mapster → CPM | **Implemented** | `Directory.Packages.props:27` one `<PackageVersion Include="Mapster" Version="7.4.0"/>`; `Mapster.DependencyInjection` absent (exit 1); three-form csproj version grep all zero (exit 1) |
| 2 | Per-module Mapster DI | **Implemented** | `GlobalSettings.Scan(...)` in both `Fokus.API/DependencyInjection.cs:25` and `Identity.API/DependencyInjection.cs:15`; no `AddMapster` (exit 1) |
| 3 | Split 16 fat endpoint files | **Implemented** | Definition-line grep in `*Endpoint.cs` → zero (exit 1); no `*Models.cs` remains (exit 1); 16 `*Command.cs`/`*Query.cs` siblings created incl. the two critic-added (`GetDevelopers`, `GetCycleTimeBoundaries`) |
| 4 | Rename `*Request`→Command/Query + validators | **Implemented** | `class \w+Request` in feature trees → zero (exit 1); `RequestValidator` anywhere in src → zero (exit 1); 37 types (impl.md) — matches the live count |
| 5 | gRPC + Jira contract renames (Q1 MAXIMAL) | **Implemented** | `TeamRequest\|SprintRequest\|UpdateJiraSprintRequest` → zero (exit 1); new names in all consumers; **wire-safe**: `TeamQuery` Order=1, `SprintQuery` Order=1,2 (identical to originals); `RestApiJiraClient.cs:63` construction updated |
| 6 | Path-A field renames + Vue client (Q2) | **Implemented** | command fields entity-aligned (`BugRatioAlertThreshold`, `BugRatioConsecutiveSprintCount`, `CycleTimeStartStage`, `CycleTimeEndStage`); client payloads send new camelCase keys; **false-positive fence intact** (`workloadAlert.consecutiveSprintCount` untouched); no stale request keys in `settings.ts` (exit 1) |
| 7 | Apply Mapster to trivial-map handlers (Q3) | **Implemented** | 5 `.Adapt(existing)` calls + 5 `IRegister` classes beside Settings slices; **excluded handlers correctly NOT Adapted** — `SaveXraySettings` keeps `IsNullOrEmpty` secret guard, `SaveCycleTimeBoundaries` has zero `.Adapt` |
| 8 | Verification sweep + both builds | **Implemented** | `dotnet build SprintRituals.slnx` → "Build succeeded" (fresh, no `error CS`); `npm run build` → `vue-tsc -b && vite build` ✓ built in 1.17s, 0 TS errors (fresh) |

## Deviations (Pass — reason valid)

- **Step 6 — `SaveCycleTimeBoundariesResponse` record fields renamed to `CycleTimeStartStage`/`CycleTimeEndStage`.** The plan *recommended leaving the PUT response keys unchanged* but explicitly flagged this as a developer-time judgment with a flag-to-architect option (plan Step 6 / Open Questions). The developer renamed them AND updated the matching consumers together (`types/index.ts` `SaveCycleTimeBoundariesResponse`, `WorkflowTab.vue` save-response handler) — internally consistent, documented in implementation.md:87/93-94/98. **Disposition: Deviated-with-reason → Pass.** Note the GET read response (`GetCycleTimeBoundariesResponse` in the `GetCycleTimeBoundaries` slice) is a separate type and its `StartStage`/`EndStage` keys were correctly left alone (impl.md:98) — the read contract is stable.

## Scope-creep check
No unexpected files. All created/modified files map to a plan step. `global using Mapster;` added to `Fokus.API/GlobalUsings.cs` (Step 7, reasonable — avoids per-endpoint usings) — not scope creep.

## Notes for Step 2 (reviewer)
This done check is conformance only (no code reading beyond the targeted accept-greps). The reviewer should still confirm at code level:
- Mapster `IgnoreNonMapped(true)` in the 5 `IRegister` classes actually prevents `.Adapt(existing)` from nulling untouched `AppSettings` fields (impl.md:134 claims this — the data-loss risk if wrong is real, since `AppSettings` is one flat row with ~20 fields and the commands map only a subset).
  > **⚠️ ERRATUM (2026-06-07):** This framing pointed the reviewer at the wrong risk. The danger was never "`.Adapt` nulls the other fields" (it doesn't — default Mapster leaves unmatched destination members untouched). The real bug is that `IgnoreNonMapped(true)` with no `.Map()` calls maps **nothing**, so `.Adapt(existing)` was a no-op and the 4 settings endpoints silently failed to persist. Verified against Mapster 7.4.0; the 4 `*Mappings.cs` were deleted. See `adhoc-SettingsMapsterNoOp`.
- The two-source `UserResponse` (UpdateRole/UpdateStatus) was left hand-assembled (correct per plan) and still compiles/behaves.
- gRPC server still starts (h2c) and Jira sprint-update still round-trips — the wire-safety claim is structurally verified (Order numbers preserved) but a runtime smoke is the reviewer's/user's call.

**Verdict: PASS → proceed to Step 2 (reviewer code review).**
