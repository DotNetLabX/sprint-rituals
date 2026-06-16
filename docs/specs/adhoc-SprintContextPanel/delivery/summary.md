# Sprint Context Panel (RA4) — Summary

## Status: COMPLETE

## What Was Built
The **backend half** of RA4: Reflekt can now pull anonymized, already-computed Fokus sprint metrics over gRPC for the sprints a retro references. Delivered the `AnonymizedSprintSummary` contract enrichment (+7 additive `[DataMember]` fields: sprint identity + per-rate RAG bands), the Fokus server-side mapping, Reflekt's **first** gRPC client registration, a retro-repo eager-load method, and the `GET /api/retro-sessions/{retroSessionId}/sprint-context` endpoint with a `GetSprintContextAssembler` (per-sprint gRPC pull, snapshot fallback, freshness states, Rule 6 disruption gate, nulls-last ordering). The Vue slide-out panel itself is **deferred to RA5** by user decision (the Reflekt SPA is not yet in this repo).

## Key Outcomes
- **Files:** ~7 source/config modified (`AnonymizedSprintSummary.cs`, `SprintAnalyticsGrpcService.cs`, `Host/appsettings.json`, `Reflekt.API/DependencyInjection.cs`, `Reflekt.Persistence/.../SprintRepository.cs`, `Reflekt/CLAUDE.md`, `SprintRituals.slnx`); new: `GetSprintContext/` feature (endpoint + assembler + query), new `Fokus.API.Tests` project, new `Reflekt.Persistence.Tests/SprintContext/` tests.
- **Build:** `dotnet build SprintRituals.slnx` → 0 errors (12 pre-existing NU1903 advisories, not introduced here).
- **Tests:** 90/90 green — Fokus.API.Tests 3/3, Reflekt.Persistence.Tests 14/14 (10 new), Fokus.Domain.Tests 62/62, Reflekt.Domain.Tests 7/7, Identity.API.Tests 4/4.
- **Plan review:** critic **ACCEPT** (Mode 2, plan vs spec).
- **Code review:** reviewer **APPROVED** + Codex **GO** (Standard+Codex, round 1). 0 blocking findings; 2 MEDIUM + 2 LOW all applied in a non-blocking fix pass. No rejection cycles.

## Deviations from Plan
- **Frontend deferred to RA5** (user-approved scope: Option A, backend-only RA4). The Vue panel, top-bar button, Flows 1–4 frontend ACs, and `help.tooltips.md` are not built here.
- **PC3 — thin snapshot kept:** the `cached` freshness state surfaces sprint identity/state only, no last-known metric numbers (diverges from a literal Rule 9; the RA3 `FokusSprintSnapshot` holds no metrics by ADR-019 design). Routed to RA5.
- **Rule 7 ordering:** scope-additions ordered by story-points (Fokus's current `TopScopeAdditions` order), not "most-recently-added" — the wire carries no added-date. The "+N more" overflow is "+N among the top-5-by-SP". Routed to RA5.
- **`GetSprintContextAssembler` extraction:** one extra scoped service, introduced so the assembly logic is testable without a FastEndpoints HTTP test host. Endpoint is a thin shell.

## Notes
- **RA5 carry-overs recorded in `Reflekt/CLAUDE.md` + `implementation.md`:** the Vue panel, the thin-snapshot metric cache (PC3), and the most-recently-added ordering + true-total overflow (Rule 7, needs a Fokus-side `AddedAt` + ordering change; `DisruptionMetrics.MidSprintAdditionCount` already has the true total on a different contract method).
- **Doc-accuracy fix (Codex catch):** the original docs misstated `DisruptionRag` as banding on `ScopeDisruptionRate` only; verified at `SprintSummaryAnalyzer.cs:257,497` it bands on the **combined** `DisruptionRate` (scope+bug). Corrected in 4 places. Mapping code was always correct.
- **Run health:** three plugin/runtime issues, all handled, none affecting correctness — boundary-detector false positives on named agents (owner-write misclassification), critic findings stranded behind an idle notification (salvaged), and one developer Phase-2 no-output stall (resumed successfully). Full detail in `communication-log.md` Runtime / Plugin Issues Log.
- `lessons.md` holds 5 developer lessons — unprocessed unless the learner is run.
