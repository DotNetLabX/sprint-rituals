# Pass 2 — SliceHygiene — Pipeline Summary

**Slug:** `adhoc-Pass2-SliceHygiene`
**Status:** Done — reviewer APPROVED, Codex GO (Cycle 1), Step 1 done-check PASS.
**Team mode:** Standard + Codex (Codex = independent cross-model second opinion on the implementation).
**Repo:** `this repo` (code + artifacts in one repo — unlike Pass 1's plugin-repo split).
**Scope:** MAXIMAL (user override of the architect's tight-scope recommendation — see questions.md).

## What shipped (8 steps, 105 files, +647/−576)

| # | Step | Result |
|---|------|--------|
| 1 | Mapster → CPM | `Mapster 7.4.0` added to `src/Directory.Packages.props` (core only, no `Mapster.DependencyInjection` per Q4a) |
| 2 | Mapster per-module DI | `TypeAdapterConfig.GlobalSettings.Scan(...)` registered in Fokus.API + Identity.API module DI |
| 3 | Split fat endpoint files (ADR-008) | 16 `*Endpoint.cs` split into `{Feature}Endpoint.cs` + `{Feature}Command.cs`/`Query.cs` siblings |
| 4 | Rename FastEndpoints request types (ADR-009) | 37 `*Request` → `*Command`/`*Query` (+ validators) across Fokus + Identity |
| 5 | Rename gRPC + Jira contracts (Q1 MAXIMAL) | `TeamRequest`→`TeamQuery`, `SprintRequest`→`SprintQuery` (wire-safe via `[DataMember(Order)]`); `UpdateJiraSprintRequest`→`UpdateJiraSprintCommand` (Refit serializes by property name — wire-safe) |
| 6 | Path-A field renames + Vue client (Q2) | Settings request fields aligned to entity (`AlertThreshold`→`BugRatioAlertThreshold`, `StartStage`→`CycleTimeStartStage`, etc.); `api/settings.ts`, `settingsStore.ts`, `types/index.ts`, `HealthTab.vue`, `WorkflowTab.vue` updated |
| 7 | Apply Mapster to trivial maps (ADR-010, Q3) | `req.Adapt(existing)` + `IRegister` w/ `IgnoreNonMapped(true)` on the trivial `Save*` settings handlers |
| 8 | Verification sweep | `dotnet build SprintRituals.slnx` green (0 err); `npm run build` green (0 err) |

## Key decisions (this pass)

- **MAXIMAL scope (Q1–Q5):** rename gRPC/Jira contracts too; Path-A request-field renames + Vue client updates; Mapster on `Save*` settings now (against today's flat `AppSettings`, Pass-3 VO rework acknowledged); core Mapster only + per-module manual `Scan`; `record` conversion opportunistic.
- **CRITICAL revert (user decision):** the developer additionally renamed the `SaveCycleTimeBoundaries` **PUT response** keys (`startStage`/`endStage` → `cycleTime*`) — a wire-contract change the plan said not to make silently. Codex flagged CRITICAL; user chose **revert**. Response keys restored; request-field renames kept.
- **SaveHealthConfig:** Mapster `IRegister` removed in favour of explicit property assignment for the 5 VO-derived complex fields (avoids deep-copy clobber; Pass-3 `ComplexProperty`-safe).

## Pipeline trace

| Phase | Outcome |
|-------|---------|
| Architect — plan | 8 steps; Step 3 HIGH (14→16 fat files) found by in-context critic, fixed. |
| Critic (fresh context) — plan gate | **GO** — 0 CRIT/HIGH/MED, 2 LOW. (Fixes Pass-1 PR-1: author≠reviewer enforced by TL spawning a fresh critic.) Plan committed `df142ce`. |
| Developer — implement | All 8 steps; self-committed `3f07fa7` (model: Sonnet 4.6; compacted once mid-run, recovered from disk). |
| Architect — Step 1 done check | **PASS** — 8/8 steps present/deviated-with-reason, 0 Missing; both builds independently re-run green. |
| Reviewer — Step 2 | **APPROVE** — 2 MED, 2 LOW (0 CRIT/HIGH). |
| Codex — Step 2 | **NO-GO** — 1 CRITICAL (response-key rename). |
| Developer — fix Cycle 1/3 | Revert response keys (request kept); SaveHealthConfig → explicit assignment; builds green. |
| Reviewer + Codex — re-review | Reviewer **APPROVE**, Codex **GO** (CRITICAL RESOLVED). |

Verdict cross-check (Verdict Validation): reviewer APPROVE with **zero** open CRITICAL/HIGH; Codex GO with CRITICAL RESOLVED; done-check PASS with **no** step Missing. All valid.

## Process notes (see lessons.md)

- **Developer self-committed** (`3f07fa7`) — normally the TL's job per the commit protocol. The commit was correctly scoped (only Pass 2 src/client/impl), so it was left in place; flagged for the learner.
- **Sonnet developer compacted** mid-run — subagents get the standard tier window (~200k), not the session's `[1m]`. Recovered from disk (plan + code are the source of truth); no rework needed. The agent `model:` frontmatter has no knob for the `[1m]` long-context variant.
- **Response-key rename** was a silent deviation the plan explicitly warned against — caught by Codex (CRITICAL), missed/tolerated by reviewer (MED) and done-check (deviated-with-reason). Cross-model review earned its keep here.

## Next

- **Pass 3 — DomainEnrichment** is now **Ready** (deps Pass 1 ✓ + Pass 2 ✓ cleared). It will introduce the `AppSettings` value objects (`BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, …) and may rewrite the `Save*` settings maps added this pass — that rework was acknowledged when MAXIMAL scope was chosen.
