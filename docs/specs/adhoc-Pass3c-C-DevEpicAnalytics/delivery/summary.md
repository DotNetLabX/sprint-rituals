# Summary — adhoc-Pass3c-C-DevEpicAnalytics

**Status:** Complete
**Pipeline completed:** 2026-06-08
**Verdict:** APPROVED (reviewer, 1 LOW, no CRITICAL/HIGH/MEDIUM) + GO (Codex cross-check, 1 WARN, no HIGH/CRITICAL)

---

## What Was Delivered

Pass 3c-C migrated the final 7 dev/epic/QA analytics services from `Fokus.API/Features/Analytics/` to domain calculators in `Fokus.Domain/Analytics/`, per ADR-005 (computation in Domain), ADR-006 (no API types in domain), ADR-010 (endpoint owns the response DTO). Third and final sibling of the Pass 3c migration (after 3c-A Sprint core and 3c-B delivery metrics). All 7 HTTP routes byte-identical.

**New domain calculators (7, meaningful suffix per Q2):** DeveloperThroughputCalculator, DeveloperProgressAnalyzer, DeveloperDetailCalculator, DeveloperQualityScorer, EpicProgressAnalyzer, QaWorkloadAnalyzer, QaTrendsAnalyzer — each with its `*Result.cs` domain record file.

**New shared wire file (Q1 → Option B):** `SharedAnalyticsWire.cs` — the 5 cross-endpoint structural records (`SprintSummaryItem`, `DeveloperProgressSprintInfo`, `CompletedTicketEntry`, `DayBreakdownEntry`, `StalledTicketEntry`) promoted to single ownership in the API layer.

**New wire files (5, Mapster IRegister):** GetDeveloperThroughputResponse.cs, GetDeveloperProgressResponse.cs, GetDeveloperDetailResponse.cs, GetEpicProgressResponse.cs, GetQaTrendsResponse.cs.

**Deleted services (7):** DeveloperThroughputService, DeveloperProgressService, DeveloperDetailService, DeveloperQualityService, EpicProgressService, QaWorkloadService, QaTrendsService.

**Modified:** 7 endpoints re-pointed at calculators; DependencyInjection.cs (7 service→calculator swaps); GetQaTrendsEndpoint keeps BugRatioCalculator injection alongside QaTrendsAnalyzer.

---

## Key Decisions Locked (user-decided)

- **Q1 → Option B** (user override of architect's recommended Option A): shared structural records live in one API-layer wire file `SharedAnalyticsWire.cs`, consumed by both endpoints of each pair — not per-endpoint duplication.
- **Q2 → meaningful suffix per calculator** (not blanket `*Calculator`, not the skill's blanket `*Analyzer`): Throughput/Detail → `Calculator`; Progress/Epic/Workload/Trends → `Analyzer`; Quality → `Scorer`. Skill-vs-project naming divergence logged (see N5).
- **Q3 → keep hand-written mappers** for DeveloperQuality + QaWorkload (move result records to Domain, re-point mappers; no Mapster IRegister).
- Per-calculator local sparkline types (`DeveloperQualitySparklinePointData`, `QaWorkloadSparklinePointData`) — no cross-calculator reuse (critic A1/A2 resolution; Step 11 grep guard).
- 5 DO-NOT-TOUCH `"neutral"` carve-out literals preserved (Throughput, Quality, QaWorkload×3) — not routed through MetricCardBuilder.

---

## Follow-up Items

| Severity | Finding | Location |
|----------|---------|----------|
| LOW | QaWorkloadAnalyzer retains private `BuildMetricCard` with an inlined polarity switch instead of `MetricCardBuilder.ComputeDelta` — behaviorally identical, optional dedup | QaWorkloadAnalyzer.cs:573-606 |
| Manual | Live byte-identical HTTP round-trip not run by reviewer/Codex (static review only) — **manual testing per backlog (no automated suite by decision)** | 7 analytics routes |

---

## Build

`dotnet build SprintRituals.slnx`: Build succeeded. 0 Error(s). 4 Warning(s) (all pre-existing NU1903).

---

## Process Note

The developer ran analyze→implement in a single spawn (two-phase checkpoint bypassed; gate token self-advanced). No correctness impact this run — the plan was critic-approved with zero open questions and the downstream done-check + dual review gates verified the code. Logged as N7 in the communication log (routed to learner) along with N1–N8 nexus-plugin observations.

---

## Artifacts

- Plan: docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/plan.md
- Implementation: docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/implementation.md
- Review (Step 1 done-check + Step 2 code review): docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/review.md
- Critic plan review: docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/critic-review.md
- Codex cross-check: docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/review-codex.md
- Communication log (+ Issues & Malfunctions Log N1–N8): docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/communication-log.md
