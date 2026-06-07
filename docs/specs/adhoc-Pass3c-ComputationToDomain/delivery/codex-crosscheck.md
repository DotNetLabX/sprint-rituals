**GO**

Reviewed commit `4e58c18` against `4e58c18^` and the current `src/Services/Fokus/` tree with focus on computation parity, wire stability, gRPC mixed-state wiring, and downstream symbol resolution.

| Severity | Area | File | Issue | Evidence |
|---|---|---|---|---|
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs`, `TransitionAttributionChecker.cs`, `CompletionChecker.cs`, `ExcludedDeveloperFilter.cs` | The 4 shared helpers are pure namespace moves; no formulas, conditions, or helper logic changed. | Blob diffs from `4e58c18^` to `4e58c18` show each file changed only `namespace Fokus.API.Features.Analytics;` -> `namespace Fokus.Domain.Analytics;`. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs` | `ComputeQaMetrics` and all downstream helpers preserve the original arithmetic and branch structure after the service-to-calculator rename. | Current methods at `QaMetricsCalculator.cs:5-140`, `148-443`; blob diff against parent `Fokus.API/Features/Analytics/QaMetricsService.cs` shows only result-type substitutions such as `QaMetricsResult` -> `QaMetricsData` and `MetricCard` -> `QaMetricCardData`, with no formula edits in `ComputeCoverageRate`, `ComputeExecutionRate`, `ComputePassRate`, `ComputeBugsFound`, `ComputeFailingCount`, `BuildQaSparkline`, or `BuildQaMetricCard`. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs` | `ComputeSummary`, `ComputeMetrics`, health-score weighting, sparkline deltas, epic progress, leaderboard, and flag computation were relocated without behavioral edits. | Current methods at `SprintSummaryCalculator.cs:5-149`, `201-260`, `445-503`, `508-558`, `566-624`, `629-683`, `686-758`; blob diff against parent `Fokus.API/Features/Analytics/SprintSummaryService.cs` shows type renames plus direct calls to `HealthScoreCalculator`, but no changed numeric operations or conditional branches. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineCalculator.cs` | Timeline, crunch, post-sprint, untested-at-close, and dev-to-test-gap calculations remain identical after relocation. | Current methods at `TestTimelineCalculator.cs:5-85`, `88-178`, `181-262`, `265-336`, `339-409`, `412-517`, `519-595`; blob diff against parent `Fokus.API/Features/Analytics/TestTimelineService.cs` shows only record-type renames (`*Entry`/`*Result` -> `*Data`) and no changed guards or formulas. |
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsQuery.cs` | `QaMetricsResponse` stayed wire-stable. | `GetQaMetricsQuery.cs:26-45` matches the pre-migration response shape; the file diff for this endpoint-owned contract adds only `using Fokus.API.Features.Analytics.GetSprintSummary;` at `:1-2`. |
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` | `SprintSummaryResponse` and its nested DTOs keep the same field names, scalar types, list types, and nullability. | Current wire records are at `GetSprintSummaryQuery.cs:27-120`. Compared with the parent top-of-file records in `Fokus.API/Features/Analytics/SprintSummaryService.cs`, the shapes are unchanged; `FlagsResult.TestingCrunch` now uses wire-local `TestingCrunchResult` at `:100-112`, but it preserves the same three serialized members previously exposed via `TestingCrunchFlag`. |
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineResponse.cs` | `TestTimelineResponse` and all nested wire records are preserved, and the renamed domain result types are explicitly mapped back to the original response shape. | Wire records remain at `GetTestTimelineResponse.cs:8-90`; `TestTimelineMap.Register` at `:94-108` maps every renamed domain record to its original wire counterpart, and `DependencyInjection.cs:26` scans the API assembly for `IRegister` configurations. |
| OK | Wire Stability | `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`, `DisruptionMetrics.cs`, `CarryOverHistory.cs` | The gRPC contracts were not modified by this pass. | `git diff --name-only 4e58c18^ 4e58c18 -- src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/...` returns no changes; `AnonymizedSprintSummary.cs:8-37` still carries the same `[DataContract]` / `[DataMember(Order = ...)]` layout. |
| OK | gRPC Mixed-State | `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Mixed injection is consistent: gRPC now consumes `SprintSummaryCalculator` and `QaMetricsCalculator` while leaving `ScopeChangeService` and `CarryOverService` in place. | Constructor wiring at `SprintAnalyticsGrpcService.cs:19-28`; summary path at `:30-52`; QA loader at `:158-169`; disruption/carry-over paths remain on the not-yet-migrated services at `:55-108` and `:257-279`. |
| OK | Build Regressions | `src/Services/Fokus/Fokus.API/DependencyInjection.cs` | Service registration and call-site retargeting are internally consistent; no live references to the deleted `SprintSummaryService`, `QaMetricsService`, or `TestTimelineService` remain. | Registrations switch to calculators at `DependencyInjection.cs:42-54`. Repo-wide searches only return live calculator references (`DependencyInjection.cs:42-44`, `GetQaMetricsEndpoint.cs:13`, `GetSprintSummaryEndpoint.cs:12-15`, `GetTestTimelineEndpoint.cs:13`, `SprintAnalyticsGrpcService.cs:25-28`) and no code references to the removed service classes. |
| INFO | Build Regressions | `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` | `TestTimelineCalculator` is still injected even though the endpoint now calls the static `TestTimelineCalculator.ComputeCrunchFlag(...)` method directly. This is dead DI, not a behavioral regression. | Constructor parameter at `GetSprintSummaryEndpoint.cs:7-15`; actual use is the static call at `:122-126`. |
| INFO | Build Regressions | `SprintRituals.slnx`, `src/Services/Fokus/Fokus.API/Fokus.API.csproj` | Full compile verification was not obtainable in this sandbox: `dotnet build` exited with code `1` during restore/build orchestration, but emitted `0 Warning(s)` and `0 Error(s)`. | `dotnet build SprintRituals.slnx` and `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj --no-restore` both terminated with no compiler diagnostics; static symbol audit found no unresolved old call sites. |

## Behavioral Parity

I compared the parent service implementations to the new domain calculators at method level, not by name. The moved helpers are namespace-only relocations, and the 3 migrated calculators keep the same formulas, rounding, delta logic, branch conditions, and edge-case guards.

| Severity | Area | File | Issue | Evidence |
|---|---|---|---|---|
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs`, `TransitionAttributionChecker.cs`, `CompletionChecker.cs`, `ExcludedDeveloperFilter.cs` | Shared helpers are logic-identical after the move. | Blob diffs show namespace-only edits. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs` | QA formulas, thresholds, deltas, and sparkline math are unchanged. | `QaMetricsCalculator.cs:5-140`, `148-443`; parent/new blob diff shows type substitutions only. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs` | Sprint summary metrics, health score composition, leaderboard, epics, and flags are unchanged. | `SprintSummaryCalculator.cs:5-149`, `201-260`, `445-503`, `508-758`; parent/new blob diff shows renamed DTO types only. |
| OK | Behavioral Parity | `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineCalculator.cs` | Timeline crunch/untested/gap logic is unchanged. | `TestTimelineCalculator.cs:5-595`; parent/new blob diff shows renamed DTO types only. |

## Wire Stability

The HTTP response contracts stayed structurally stable. The endpoint folders now own the wire DTOs, but the payload fields, scalar/list types, nullability, and gRPC serialization attributes remain intact. I also checked the protobuf contracts directly and confirmed they were not touched in this commit.

| Severity | Area | File | Issue | Evidence |
|---|---|---|---|---|
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsQuery.cs` | `QaMetricsResponse` remained unchanged. | `GetQaMetricsQuery.cs:26-45`; file diff adds only a `using`. |
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` | `SprintSummaryResponse` and nested DTO shapes are preserved. | `GetSprintSummaryQuery.cs:27-120`; nested `TestingCrunch` shape remains `decimal + int + int` at `:100-112`. |
| OK | Wire Stability | `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineResponse.cs` | `TestTimelineResponse` and nested DTO shapes are preserved. | `GetTestTimelineResponse.cs:8-90`; explicit mapping back from renamed domain types at `:94-108`. |
| OK | Wire Stability | `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`, `DisruptionMetrics.cs`, `CarryOverHistory.cs` | gRPC contract definitions are unchanged. | No diff in those files; `AnonymizedSprintSummary.cs:8-37` retains the same `[DataMember(Order = ...)]` declarations. |

## gRPC Mixed-State

The mixed state in `SprintAnalyticsGrpcService` is coherent. The migrated summary/QA computation now goes through domain calculators, while disruption and carry-over still route through the not-yet-migrated API services. I did not find missing constructor params, duplicated QA computation, or dropped execution paths.

| Severity | Area | File | Issue | Evidence |
|---|---|---|---|---|
| OK | gRPC Mixed-State | `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Constructor wiring matches the intended mixed state. | `SprintAnalyticsGrpcService.cs:19-28` injects `SprintSummaryCalculator`, `QaMetricsCalculator`, `ScopeChangeService`, and `CarryOverService`. |
| OK | gRPC Mixed-State | `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Summary routing is single-path and complete: load QA once, compute summary once, then map to `AnonymizedSprintSummary`. | `SprintAnalyticsGrpcService.cs:30-52`, `:158-169`, `:201-225`. |
| OK | gRPC Mixed-State | `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Disruption and carry-over paths still route to the pre-migration services, as intended for 3c-A. | `SprintAnalyticsGrpcService.cs:55-108`, `:244-279`. |

## Build Regressions

I checked the DI registrations, grep’d for stale call sites, and verified that the broad helper `using` churn now resolves to `Fokus.Domain.Analytics`. I did not find any confirmed namespace-collision or missed-call-site regression. The only code-level note is one now-unused DI parameter in `GetSprintSummaryEndpoint`. Full `dotnet build` verification was inconclusive in this sandbox because the CLI failed without compiler diagnostics.

| Severity | Area | File | Issue | Evidence |
|---|---|---|---|---|
| OK | Build Regressions | `src/Services/Fokus/Fokus.API/DependencyInjection.cs` | Calculator registrations replaced the deleted services cleanly. | `DependencyInjection.cs:42-54`. |
| OK | Build Regressions | `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Helper references were retargeted safely; fully-qualified `ExcludedDeveloperFilter` calls avoid namespace ambiguity. | `SprintAnalyticsGrpcService.cs:73-74`, `:148-149`, `:192-193`. |
| INFO | Build Regressions | `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` | `TestTimelineCalculator` instance injection is no longer needed because the endpoint uses the static helper entry point. | `GetSprintSummaryEndpoint.cs:7-15`, `:122-126`. |
| INFO | Build Regressions | `SprintRituals.slnx`, `src/Services/Fokus/Fokus.API/Fokus.API.csproj` | Sandbox build execution did not yield a reliable compile verdict. | `dotnet build` exited `1` with `0 Warning(s)` and `0 Error(s)` in both solution-level and project-level invocations. |

## Summary

BLOCKERs: `0`  
WARNs: `0`

Verdict rationale: the migrated computation code is a relocation rather than a rewrite, the HTTP/gRPC wire shapes remain stable, and the gRPC mixed-state wiring is internally consistent. The remaining notes are non-functional (`GetSprintSummaryEndpoint` has one unused DI parameter, and sandbox build execution did not provide a trustworthy compiler signal), so they do not justify blocking 3c-A.
