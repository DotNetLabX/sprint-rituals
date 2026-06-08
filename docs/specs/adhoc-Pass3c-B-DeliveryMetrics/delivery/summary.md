# Summary — adhoc-Pass3c-B-DeliveryMetrics

**Status:** Complete
**Pipeline completed:** 2026-06-08
**Verdict:** APPROVED (reviewer, no CRITICAL/HIGH findings)

---

## What Was Delivered

Pass 3c-B migrated 5 delivery-metric analytics services from `Fokus.API/Features/Analytics/` to domain calculators in `Fokus.Domain/Analytics/`, per ADR-005 (computation in Domain) and ADR-010 (endpoint-owns-response-DTO). Continuation of Pass 3c-A.

**New domain calculators:** CarryOverCalculator, CycleTimeCalculator, BugRatioCalculator, LeaderboardCalculator, ScopeChangeCalculator

**New shared domain primitives:** MetricCardData (6-field sealed record), MetricCardBuilder (static kernel — string-polarity + bool-polarity paths)

**New wire files (5):** GetCarryOverResponse.cs, GetCycleTimeResponse.cs, GetBugRatioResponse.cs, GetLeaderboardResponse.cs, GetScopeChangeResponse.cs — each with full Mapster IRegister config

**Deleted services (5):** CarryOverService.cs, ScopeChangeService.cs, CycleTimeService.cs, BugRatioService.cs, LeaderboardService.cs

**Modified:** 2x 3c-A calculators (SprintSummary, QaMetrics) delegate direction/polarity builders to MetricCardBuilder.ComputeDelta; SprintAnalyticsGrpcService retired mixed state (now uses domain calculators); DependencyInjection.cs swapped 5 service to calculator registrations; GetQaTrendsEndpoint (justified deviation — undocumented BugRatioService injection caught during deletion sweep).

---

## Key Decisions Locked

- Q2/Q5 maximize sharing: MetricCardData + MetricCardBuilder shared across all 7 calculators (5 3c-B + 2 3c-A). SprintMetricCardData/QaMetricCardData isolated (carry Sparkline + Rag).
- Plan carve-out: LeaderboardCalculator.cs:141 TotalSpDeltaPolarity kept as hard-coded literal "neutral".
- ScopeMetricCard declared in 2 endpoint namespaces (GetCarryOver + GetScopeChange) — wire stability.
- Action == "Added" latent case bug in gRPC preserved verbatim — not fixed in this pass.

---

## Follow-up Items

| Severity | Finding | Location |
|----------|---------|----------|
| LOW | CycleTimeCalculator.BuildMetricCards private wrapper name uses old convention | CycleTimeCalculator.cs:395 |
| LOW | Domain result record collection properties use List<T> instead of IReadOnlyList<T> | All 5 *Result.cs domain files (pre-existing from 3c-A) |

---

## Build

dotnet build SprintRituals.slnx --no-incremental: Build succeeded. 0 Error(s). 6 Warning(s) (all pre-existing).

---

## Artifacts

- Plan: docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/plan.md
- Implementation: docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/implementation.md
- Review: docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/review.md
- Communication log: docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/communication-log.md
