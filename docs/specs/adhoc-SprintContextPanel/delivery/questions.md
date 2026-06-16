# Sprint Context Panel — Questions

## Q1: The Reflekt frontend SPA does not exist in this repo — where does the panel's UI land?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `docs/specs/adhoc-SprintContextPanel/definition/spec.md` (Out of Scope: "the panel lives in the existing Reflekt SPA")

**Context:** The spec's frontend half (Flows 1–4, the top-bar button, the slide-out panel, `help.tooltips.md`) assumes "the existing Reflekt SPA." That SPA is **not in this repository**. The single `client/` Vue app is the **Fokus** SPA (SprintsView, dashboard, qa, epics, developers). RA1 (`adhoc-ReflektLiftAndShift`) lifted the Reflekt **backend module only** — its summary (`docs/specs/adhoc-ReflektLiftAndShift/delivery/summary.md:53`) lists "frontend (RA5)" as a deferred carry-over. The Reflekt retro UI lives read-only in `<reflekt-repo>/web/reflekt-web/src/components/retro/` (the original project). The backlog (`docs/backlog.md:100-101,105`) confirms RA5 (`adhoc-ReflektFrontendMerge`) brings the Reflekt SPA into the monolith, and that **RA4 depends only on RA3, not RA5**.

**Question:** With no Reflekt SPA in the repo, where does the sprint-context panel UI land — defer it to RA5 and build only the backend now, reorder RA5 ahead of RA4, or land it into the original (read-only) Reflekt project?

**Recommendation:** Option A — **backend-only RA4**. Build the `GET .../sprint-context` endpoint, the Reflekt-side `ISprintAnalyticsService` gRPC client, and the `AnonymizedSprintSummary` per-rate-RAG enrichment now; defer the Vue panel + top-bar button + Flows 1–4 frontend ACs + `help.tooltips.md` to RA5. This matches the stated dependency graph (RA4←RA3 only), delivers the cross-module integration payoff (anonymized gRPC pull proven end-to-end), and leaves a clean RA5 frontend slice. Rejected alternatives: (B) reorder RA5 ahead — larger scope, changes program sequencing; (C) land into the original Reflekt SPA — that project is read-only per CLAUDE.md and isn't built/served by this repo.
**Confidence:** high — the dependency graph and RA1's deferral are documented; A is the only path consistent with both without reopening program sequencing.

### Answer
**user answered (relayed by team-lead): Option A — backend-only RA4.** Build now: the `GET .../sprint-context` endpoint, the Reflekt-side gRPC client registration, and the `AnonymizedSprintSummary` contract enrichment (per-rate RAGs). Defer to RA5: the Vue slide-out panel, the top-bar button, the Flows 1–4 frontend ACs, and `help.tooltips.md` — stated explicitly in the plan. The two contract enrichments and the snapshot-cache fork remain the architect's planner calls, made explicit in the plan with reasoning. Note: with the UI deferred there is no UI consumer of the `cached`-vs-degraded snapshot state yet — this informs (does not dictate) the snapshot-cache fork.
