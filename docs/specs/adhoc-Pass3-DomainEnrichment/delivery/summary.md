# Summary — adhoc-Pass3-DomainEnrichment (Pass 3a: No-DTO-in-Domain)

**Status:** ✅ Complete · **Mode:** Standard + Codex · **Branch:** `main`

Pass 3 (DomainEnrichment) was split into three sequential sub-passes at launch; **this run delivered Pass 3a only** (ADR-006). 3b and 3c are separate future runs with decisions already locked (see Locked Decisions below + `communication-log.md`).

## What was built (ADR-006 — no external DTO inside the Domain layer)

- Relocated the **8 `From{Jira,Xray}` factory methods** off the Fokus domain aggregates into boundary mappers:
  - `Fokus.API/Features/Sync/JiraDomainMapper.cs` — Mapster `IRegister` (Sprint, Ticket, Developer, StatusTransition, SprintMembership).
  - `Fokus.API/Features/Xray/XrayDomainMapper.cs` — plain construction (TestRun, TestExecution, TestSet).
- **Dropped the `Fokus.Domain → Jira.Contracts` project reference** and the stale `global using Jira.Contracts;` (Domain + Persistence + API global usings cleaned; 8 per-file usings added where API code still needs Jira types).
- Repointed the **13 call sites** in `SprintIssueSyncService` / `XrayIssueSyncService` to the new mappers; deleted the 8 domain factories.
- Corrected the stale `JiraMapper` line in `src/Services/Fokus/CLAUDE.md`.

**Deviation (documented, in-scope):** moved the `SprintState` enum into `Fokus.Domain/Sprint/SprintState.cs` — `Sprint.State` was typed as `Jira.Contracts.SprintState`, so the reference could not be dropped without it. Jira-API filter usages were explicitly re-qualified as `Jira.Contracts.SprintState`. Confirmed behavior-preserving by reviewer + Codex.

## Gates (all passed)

| Gate | Verdict | Artifact |
|------|---------|----------|
| Critic — plan review | **APPROVE** (no CRITICAL/HIGH; 1 MED + 2 LOW advisory) | `review.md` |
| Architect — Step 1 done-check | **PASS** — all 6 steps done, `dotnet build` 0 errors | `done-check.md` |
| Reviewer — Step 2 code review | **APPROVED** (no CRITICAL/HIGH; 2 LOW follow-ups) | `review.md` §Step 2 |
| Codex — adversarial review | **GO** (no BLOCKER/MAJOR; parity confirmed) | `codex-review-impl.md` |

**Build:** `dotnet build SprintRituals.slnx` → `Build succeeded. 6 Warning(s), 0 Error(s)` (warnings pre-existing, none from this pass).

## Files changed

26 files, +354 / −223 (commit `d68f493`). New: `JiraDomainMapper.cs`, `XrayDomainMapper.cs`, `SprintState.cs`. Deleted factories across 8 domain behavior files. `Fokus.Domain` now references **only** `Blocks.Domain`.

## Process note

The developer agent collapsed Phase 1→2 and self-committed the implementation in one run (the two-phase checkpoint was bypassed). No new blocking questions were missed (it found "all clear"), and the skipped review gates (done-check, code review, Codex) were run afterward by the team lead — all passed. Captured as a lesson in `lessons.md`.

## Non-blocking follow-ups (future passes)

- **F1 [LOW]** `MapStatusTransitions` returns `List<T>` not `IReadOnlyList<T>` — bound by `TicketRepository.ReplaceTransitionsAsync`'s parameter; fix needs a paired repo-signature change.
- **F2 [LOW]** Two `SprintState` enums (domain + `Jira.Contracts`) coexist — expected; the boundary mapper bridges them.

## Locked decisions for the remaining sub-passes

- **3b** (ADR-007) — group the ~12 remaining `AppSettings` scalars into config VOs; encapsulate Identity `Person`/`User`; Fokus sync aggregates stay anemic (honors B3 upsert).
- **3c** (ADR-005) — full migration of all 15 analytics services to `Fokus.Domain/Analytics` calculators returning domain result records; endpoint owns a separate response DTO + Mapster map even for 1:1 shapes.
