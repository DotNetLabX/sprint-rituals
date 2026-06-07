# Pass 3c — ComputationToDomain — Questions

Phase 1 analysis questions. Scope source: `docs/backlog.md` (Pass 3c row "Migrate all 15 analytics services to Fokus.Domain calculators + endpoint-owned DTOs (ADR-005)") + `docs/architecture/decisions.md` **ADR-005/010**; decisions locked in `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/questions.md` Q2 (full migration of all 15, endpoint-owns-DTO even for 1:1 shapes) and Q5 (skills are sufficient, no reference-project re-inspection). No `spec.md` — adhoc refactoring pass.

**Evidence gathered (Phase 1, this session):**
- 15 analytics `*Service` classes registered in `Fokus.API/DependencyInjection.cs`. 4 static utility classes also live in `Fokus.API/Features/Analytics/`: `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter`.
- Each `*Service.cs` file declares both: (a) response/entry/card records that ARE the wire shape today, and (b) computation logic that returns those records. Under ADR-005/010: the computation logic moves to domain calculators returning domain result records; the endpoint owns the response DTO + Mapster map.
- `SprintAnalyticsGrpcService` is a **cross-cutting consumer** of 4 services: `SprintSummaryService`, `ScopeChangeService`, `CarryOverService`, `QaMetricsService`. It references response records by name (`SprintSummaryResponse`, `QualityBreakdownResult`, `FlagsResult`, `TestingCrunchFlag`, `MetricCard`, `MetricsResult`, etc.) and maps them to gRPC contract types. After migration, the gRPC service must adapt to use the new domain result types.
- `TestingCrunchFlag` is declared in `TestTimelineService.cs` but consumed by both `GetSprintSummaryEndpoint` and `SprintAnalyticsGrpcService` — it needs to move to a domain type.
- The plan will exceed 10 steps; auto-split is required (each sub-plan >= 2 steps).

---

## Q1: Plan split — how to cluster the 15 services into sub-plans?
**From:** architect
**To:** user
**Status:** Answered — 3c-A/B/C as 3 separate pipeline runs approved
**Step:** Phase 1 analysis

**Context:** ADR-005 requires migrating all 15 analytics services from `Fokus.API/Features/Analytics/` to domain calculators in `Fokus.Domain/Analytics/`. With 15 services + response-record decomposition + Mapster wiring + gRPC adaptation + DI update + verification, the plan will have well over 10 steps — the auto-split threshold. A split is mandatory when each sub-plan would have >= 2 steps (and all would).

Three logical clusters naturally emerge from the service relationships:

**Cluster A — Cross-cutting infrastructure + Sprint core (sub-plan 3c-A):**
- Static helpers to domain: `HealthScoreCalculator` → `Fokus.Domain/Analytics/HealthScoreCalculator.cs`, `TransitionAttributionChecker` → `Fokus.Domain/Analytics/TransitionAttributionChecker.cs`, `CompletionChecker` → `Fokus.Domain/Analytics/CompletionChecker.cs`, `ExcludedDeveloperFilter` → `Fokus.Domain/Analytics/ExcludedDeveloperFilter.cs`
- Sprint summary service: `SprintSummaryService` → `Fokus.Domain/Analytics/SprintSummaryCalculator.cs`
- QA metrics service: `QaMetricsService` → `Fokus.Domain/Analytics/QaMetricsCalculator.cs`
- TestTimeline service: `TestTimelineService` → `Fokus.Domain/Analytics/TestTimelineCalculator.cs` (defines `TestingCrunchFlag` which is cross-consumed by SprintSummary + gRPC)
- gRPC service adaptation (4 services consumed by gRPC; adapt after the domain types land)

**Cluster B — Delivery metrics (sub-plan 3c-B):**
- `CarryOverService` → `CarryOverCalculator`
- `ScopeChangeService` → `ScopeChangeCalculator`
- `CycleTimeService` → `CycleTimeCalculator`
- `BugRatioService` → `BugRatioCalculator`
- `LeaderboardService` → `LeaderboardCalculator`

**Cluster C — Developer + Epic analytics (sub-plan 3c-C):**
- `DeveloperThroughputService` → `DeveloperThroughputCalculator`
- `DeveloperProgressService` → `DeveloperProgressCalculator`
- `DeveloperDetailService` → `DeveloperDetailCalculator`
- `DeveloperQualityService` → `DeveloperQualityCalculator`
- `EpicProgressService` → `EpicProgressCalculator`
- `QaWorkloadService` → `QaWorkloadCalculator`
- `QaTrendsService` → `QaTrendsCalculator`

**Question:** Do you approve this 3-cluster split (A/B/C as separate pipeline runs), or prefer a different grouping? Alternatively: should 3c be a single large plan (one pipeline run) with sub-plan parts A/B/C as internal groupings under one `plan.md`?

My recommendation: **3 separate pipeline runs (3c-A, 3c-B, 3c-C)** — the clusters are independent (B and C don't depend on each other; they both depend on A's infrastructure), and each is ~5-7 steps, well within the 10-step threshold. The gRPC adaptation in 3c-A unblocks B and C. This mirrors how 3a/3b/3c split Pass 3.

---

## Q2: gRPC adaptation scope — rename or keep response records as both wire + domain type?
**From:** architect
**To:** user
**Status:** Answered — A (clean break): domain calculators return new domain result records; gRPC service maps from domain types
**Step:** Phase 1 analysis

**Context:** `SprintAnalyticsGrpcService` calls `SprintSummaryService`, `ScopeChangeService`, `CarryOverService`, `QaMetricsService` and references their response records by name (`SprintSummaryResponse`, `MetricsResult`, `MetricCard`, `FlagsResult`, `QualityBreakdownResult`, `QualityBreakdownInfo`, `TestingCrunchFlag`, `CarryOverMultiSprintResponse`, etc.) to map them to gRPC contract types (`AnonymizedSprintSummary`, `DisruptionMetrics`, `CarryOverHistory`).

After migration, the services become domain calculators returning domain result records. The gRPC service must be updated to use the domain types. Two approaches:

**(A) Clean break:** Domain calculators return new domain result records (named `*Result`, `*Data`, not `*Response`); endpoints own response records; gRPC service maps from domain result records to gRPC contracts. Full ADR-010 compliance — domain and wire evolve independently.

**(B) Minimal rename:** Promote the existing `*Response`/`*Entry`/`*Card` records into the domain layer with their current names (or thin renames), so the gRPC service's mapping code needs minimal changes. Faster short-term; but keeps API-flavored record names in the domain.

**Question:** Which approach — (A) clean new domain result records (adds mapping work in the gRPC service) or (B) promote existing records with minimal rename?

My recommendation: **(A)** — ADR-010 is explicit ("The domain type and the wire contract evolve independently; do not return the domain type directly from the endpoint"), and Q2 from Pass 3 locked "endpoint-owns-a-separate-response-DTO rule (D-B/ADR-010) applies even for 1:1 shapes." The gRPC service is a second consumer of domain results, same as an endpoint — it should map from domain types to its contract types, not receive pre-shaped response DTOs.
