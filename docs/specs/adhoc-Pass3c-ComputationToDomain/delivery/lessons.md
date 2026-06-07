# Pass 3c — ComputationToDomain — Lessons

## Architect Lessons

- **Multi-consumer response records need explicit decomposition contracts before planning.** `SprintSummaryService.cs` defines records consumed by both the HTTP endpoint and `SprintAnalyticsGrpcService`. When migrating to domain result records, both consumers need updating — overlooking the gRPC consumer would leave it broken mid-migration. Always grep for record usages across the codebase before scoping the response-record decomposition.
- **Static utility classes also violate ADR-005 location rule.** `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, and `ExcludedDeveloperFilter` are static classes in `Fokus.API/Features/Analytics/` but contain pure domain logic. They are effectively domain helpers misplaced in the API layer. The plan must include moving these alongside the service-class migration.
- **`TestingCrunchFlag` is a cross-cutting domain type defined in `TestTimelineService.cs`.** Moving `TestTimelineService` to a domain calculator must also move `TestingCrunchFlag`, which is consumed by `SprintSummaryService` (pass-through to the response) and `SprintAnalyticsGrpcService`. Ordering: `TestTimelineCalculator` + `TestingCrunchFlag` domain type must land before `SprintSummaryCalculator` references it.
- **Plan split threshold:** 15 services + static helpers + response-record decomposition + gRPC adaptation well exceeds the 10-step split threshold. Phase 1 correctly surfaced the split question before writing an oversized plan.
