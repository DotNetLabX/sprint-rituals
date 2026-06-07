# Summary — adhoc-Pass3c-ComputationToDomain (Pass 3c-A)

**Status:** Complete · **Branch:** main · **Team mode:** Standard+Codex · **Review mode:** Critic

## What was built

Pass 3c-A — the Sprint-core arm of the ADR-005/010 analytics migration. Relocated the Sprint-core analytics computation out of API service classes into `Fokus.Domain/Analytics/` calculators that return domain result records, with the endpoints (and the gRPC service) owning and mapping to the wire contracts.

- **New domain calculators** (`Fokus.Domain/Analytics/`, ~2,366 lines): `SprintSummaryCalculator`, `QaMetricsCalculator`, `TestTimelineCalculator` + result records (`SprintSummaryResult`, `QaMetricsResult`, `TestTimelineResult`).
- **4 static helpers** moved to the domain: `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter`.
- **7 old API-layer files deleted**; endpoints (`GetSprintSummary`, `GetQaMetrics`, `GetTestTimeline`) rewired to call calculators and map to endpoint-owned response DTOs (new `GetTestTimelineResponse.cs`; wire types consolidated in the `GetSprintSummary` slice).
- **gRPC adaptation:** `SprintAnalyticsGrpcService` consumes the 2 migrated calculators and maps from domain types; `ScopeChange`/`CarryOver` remain on the old services (mixed state, intentional until 3c-B).
- **`using` fallout** fixed across ~22 downstream 3c-B/3c-C-scope files (namespace-only, no logic change). `CLAUDE.md` updated for the domain-calculator pattern.

**Clean break (ADR-010):** domain returns new `*Result`/`*Data` records; no wire-flavored names in the domain. **Wire stability invariant held:** all HTTP response contracts and the gRPC `AnonymizedSprintSummary` contract are byte-for-byte unchanged. **No DB migrations** — pure code relocation.

## Quality gates

| Gate | Verdict | Notes |
|------|---------|-------|
| Critic — plan review (Mode 2) | NOT-COMPLETE: 4 MEDIUM, 5 LOW (none blocking) | Detail lost to agent malfunction (M6); accepted per user, independently re-derived downstream |
| Architect — Step 1 done-check | **PASS** | 9/9 steps Implemented, 0 Missing, `dotnet build` 0 errors |
| Reviewer — Step 2 code review | **APPROVED** | 0 CRITICAL, 0 HIGH, 4 LOW |
| Codex — cross-check | **GO** | Behavioral parity + wire/gRPC byte-stability confirmed |
| Post-review cleanup build | **0 errors** | warnings 7 → 4 (CS9113 removed; remaining 4 are pre-existing NU1903 advisories) |

## Post-review cleanup

- Removed the unused DI constructor parameter in `GetSprintSummaryEndpoint` (eliminated new **CS9113** warning).
- Sealed the new domain result records (`QaMetricsResult`, `SprintSummaryResult`; `TestTimelineResult` already sealed).
- Recorded the `List<T>` vs `IReadOnlyList<T>` wire/result-record convention call in `docs/conventions/known-deviations.md` (accepted, not changed).

## Changes

- Implementation commit `4e58c18`: 44 files, +790 / −384.
- Plan commit `9ca64e7`.
- Finalize commit (this): post-review cleanup (4 files, +17/−17) + delivery artifacts + backlog.

## Follow-ups

- **3c-B** (5 delivery metrics) and **3c-C** (7 dev/epic/QA) — both unblocked, marked Ready in the backlog. They reuse 3c-A's consolidated shared wire types and will retire the remaining gRPC mixed-state.
- **Pass 5** (event correctness) now depends on 3c-B + 3c-C.

## Process note

This run logged **11 agent/tooling malfunctions** (minimal-return violations, an unenforced `pipeline-gate`, a self-committing developer, a critic that couldn't surface its findings, stray-dir pollution). Net impact on the delivered code: **zero** — every gate independently confirmed correctness. Full detail in `communication-log.md` → Malfunctions Log. Candidate input for a learner pass.
