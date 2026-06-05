---
name: analytics-computation-service
description: Creates an analytics computation service — records, delta/direction/polarity helpers, multi-sprint iteration, sparkline builders, rolling average. Use when adding a new analytics tab or metric endpoint.
user-invocable: true
---

# Analytics Computation Service

Creates the backend computation service for a new analytics feature. Covers the full pattern: response records at the top, service class with multi/single-sprint methods, delta helpers, sparkline builders, and rolling average computation.

## When to use

When a plan step creates a new analytics service (e.g., `QaMetricsService`, `QaWorkloadService`, `TestTimelineService`, `DeveloperDetailService`) or extends an existing one with a new computation area.

## Reference files

These files demonstrate the established pattern:

| File | What it shows |
|------|---------------|
| `Fokus.API/Features/Analytics/BugRatioService.cs` | Multi/single split, delta helpers, alert evaluation |
| `Fokus.API/Features/Analytics/QaWorkloadService.cs` | Delta + direction + polarity triplet per column |
| `Fokus.API/Features/Analytics/QaMetricsService.cs` | Sparkline builders, metric card pattern |
| `Fokus.API/Features/Analytics/TestTimelineService.cs` | Per-day computation, static helper extraction |
| `Fokus.API/Features/Analytics/DeveloperProgressService.cs` | Gap delta, stall detection, direction derivation |
| `Fokus.API/Features/Analytics/DeveloperDetailService.cs` | Cross-service composition, rolling average |
| `Fokus.API/Features/Analytics/LeaderboardService.cs` | Ranked entries, capacity-aware skip |

## Service structure

1. **Response records at the top of the file** — all records the service returns, using `sealed record`.
2. **Service class** — primary constructor with repository dependencies.
3. **Public method(s)** — typically `ComputeMultiSprint` and/or `ComputeSingleSprint`.
4. **Private helpers** — decomposed computation methods: `BuildMetricCards`, `BuildSparkline`, `ComputeDelta`, etc.

## Key patterns

### Delta / Direction / Polarity triplet

Each numeric column that shows change has three fields:

- `{Column}Delta` (`decimal?`) — difference from prior sprint (null if no prior)
- `{Column}Direction` (`string?`) — `"up"`, `"down"`, `"flat"`, or null
- `{Column}Polarity` (`string?`) — `"positive"`, `"negative"`, `"neutral"`, or null (depends on whether higher is better for that column)

Direction is derived from delta sign. Polarity is derived from direction + column semantics (higher-is-better vs lower-is-better).

### Sparkline builder

```
BuildSparkline(sprints, windowSize, anchorIndex, valueSelector) -> SparklinePoint[]
```

Extracts a sliding window of values for a mini-chart. Each point has the sprint identifier and the metric value.

### Rolling average

Iterates over prior sprints (capacity-aware: skip sprints where developer has 0 SP), computes average of a metric over a configurable look-back window. Uses `TakeLast` AFTER any exclusion guards — not before (ordering matters to avoid returning fewer entries than requested).

### Multi-sprint vs single-sprint

- **Multi-sprint:** Accepts a list of target sprints + full closed sprint history. Iterates targets, computes per-sprint metrics, returns a list.
- **Single-sprint:** Accepts one sprint + prior sprint (nullable). Computes current values + deltas against prior. Returns a single response.

### Xray-disabled fast path

Analytics endpoints that include QA data check `appSettings.XrayEnabled` early. If disabled, return the non-QA portion of the response with QA fields nulled out. Don't query QA data at all.

## Endpoint wiring

The endpoint creates the service (or receives it via DI), loads sprint data from repositories, and calls the service method. The service never touches repositories directly — it receives pre-loaded data.

**Exception:** Some services accept repository references for targeted queries (e.g., loading test executions for a specific sprint set). This is acceptable when the query is tightly coupled to the computation.

## What this skill does NOT cover

- Frontend types, API functions, store wiring — see `create-vue-feature` skill
- Endpoint class creation — see `create-feature` skill
- Repository query methods — describe in the plan step inline
- Domain model changes — see `domain-patterns` skill
