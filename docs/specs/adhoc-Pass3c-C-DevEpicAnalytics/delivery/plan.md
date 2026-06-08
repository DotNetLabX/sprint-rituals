# Implementation Plan — Pass 3c-C: DevEpicAnalytics

**Feature Spec:** None (adhoc refactoring pass — scope from `docs/backlog.md` row 3c-C + ADR-005/010, plus the locked 3c-A/3c-B clean-break precedent)
**Slug:** adhoc-Pass3c-C-DevEpicAnalytics
**Plan:** docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/plan.md
**Skill:** `analytics-computation-service` (+ base `domain-service`)
**Intent:** Refactoring (restructure; behavior must NOT change — every HTTP response byte-identical)

---

## Context

Third and final sibling of the Pass 3c analytics-to-domain migration. Moves **7 dev/epic/QA analytics services** from `Fokus.API/Features/Analytics/*Service.cs` to domain calculators in `Fokus.Domain/Analytics/`, per **ADR-005** (computation in Domain), **ADR-006** (no DTO in domain), **ADR-010** (endpoint owns the response DTO; map domain → wire at the boundary). Continuation of Pass 3c-A (Sprint core) and Pass 3c-B (5 delivery metrics).

**In scope (7 services):** DeveloperThroughput, DeveloperProgress, DeveloperDetail, DeveloperQuality, EpicProgress, QaWorkload, QaTrends. Plus the **Option-B shared wire file** (Q1) that holds the cross-service structural records.

**Already-available shared infrastructure (3c-C creates NONE of it — it consumes):**
- `Fokus.Domain/Analytics/MetricCardData.cs` — 6-field bare card record (3c-B).
- `Fokus.Domain/Analytics/MetricCardBuilder.cs` — string-polarity path (`ComputeDelta`, `Build`) + bool-polarity path (`Direction(decimal)`/`Direction(int)`/`Polarity(decimal,bool)`/`Polarity(int,bool)`), with the `"neutral"`-string short-circuit (3c-B).
- `Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` — the **wire** records `MetricCard` (6 base fields + `List<SparklinePoint> Sparkline` + `Rag`) and `SparklinePoint(string SprintName, decimal Value)` (3c-A). These stay in the API layer; QaWorkload + DeveloperQuality reference them.
- Domain helpers already present and reused: `TransitionAttributionChecker`, `ExcludedDeveloperFilter`, `CompletionChecker`, `HealthScoreCalculator`.
- Mapster is wired: `DependencyInjection.cs:26` runs `TypeAdapterConfig.GlobalSettings.Scan(...)` over the Fokus.API assembly, so any `IRegister` in a wire file auto-registers.

**No gRPC impact.** Each of the 7 services is consumed by exactly **one** endpoint and registered **once** in DI (verified by grep — see Step 9). gRPC (`SprintAnalyticsGrpcService`) consumes only ScopeChange + CarryOver (3c-B), none of these 7. **No gRPC step in this pass.**

**No migrations.** Pure code relocation + rename. No EF schema or DB change.

---

## Locked decisions (from questions.md — all user-decided)

- **Q1 → Option B (overrides architect's Option A).** The cross-service shared **structural** records are promoted to a single shared **wire** file in the API layer, consumed by both endpoints of each shared pair. See Step 1.
- **Q2 → "most meaningful suffix" per class (overrides blanket `*Calculator`).** Per-class domain names are assigned below as **binding contracts** (do not rename in implementation):

  | Old API service | New domain class (binding) | Why this suffix |
  |-----------------|----------------------------|-----------------|
  | `DeveloperThroughputService` | **`DeveloperThroughputCalculator`** | per-sprint SP throughput/completion arithmetic |
  | `DeveloperProgressService` | **`DeveloperProgressAnalyzer`** | gap/pace/stall analysis + alert-direction derivation |
  | `DeveloperDetailService` | **`DeveloperDetailCalculator`** | assembles per-dev trend + current-sprint detail |
  | `DeveloperQualityService` | **`DeveloperQualityScorer`** | coverage/pass-rate RAG scoring + below-median streak |
  | `EpicProgressService` | **`EpicProgressAnalyzer`** | velocity projection + completion + QA analysis |
  | `QaWorkloadService` | **`QaWorkloadAnalyzer`** | per-person workload distribution + balance-alert analysis |
  | `QaTrendsService` | **`QaTrendsAnalyzer`** | trend series + Pearson defect correlation |

- **Q3 → keep hand-written mappers for DeveloperQuality + QaWorkload.** Move their result records to Domain, re-point the existing `MapResponse`/`MapMultiResponse`/`MapSingleResponse`. Do NOT convert to Mapster. Mapster-uniformity is a LOW follow-up.

---

## Wire-stability invariant (binding — applies to every step)

All seven HTTP response contracts must remain **byte-for-byte identical** (route, JSON property names, nesting, value rounding):
`/api/analytics/developer-throughput`, `/developer-progress`, `/developer-detail/{accountId}`, `/developer-quality`, `/epic-progress`, `/qa-workload`, `/qa-trends`.

Rules:
- **Wire records keep their exact names and shapes.** Renaming happens only on the **domain** side (`*Result`/`*Data`). The wire record a serializer sees is unchanged.
- **The `{ Mode, Multi?, Single? }` envelope is wire-only.** QaWorkload's `QaWorkloadResponse` (and the existing `QaWorkloadResponseDto` mode wrapper) are assembled by the endpoint; the calculator returns the two shapes (`ComputeMultiSprint`/`ComputeSingleSprint`) separately. Never mirror the envelope into the domain and never add it to a domain rename table.
- **Hard-coded `"neutral"` polarity literals are NOT `DeltaPolarity(...)` calls** — see the DO-NOT-TOUCH carve-out table below. A blanket "route every polarity through `MetricCardBuilder`" corrupts these.

---

## DO-NOT-TOUCH carve-out — hard-coded `"neutral"` literals (HIGH-risk)

These fields are **string literals occupying a polarity position**, not calls to a delta-polarity helper. They must stay verbatim `"neutral"`. Routing them through `MetricCardBuilder.Polarity(...)` (the bool path) would compute positive/negative from the sign and silently change wire output for non-flat deltas.

| File:line (current) | Field | Keep as |
|---------------------|-------|---------|
| `DeveloperThroughputService.cs:153` | `spAssignedDeltaPolarity` | literal `"neutral"` |
| `DeveloperQualityService.cs:134` | `bugsDeltaPol` | literal `"neutral"` |
| `QaWorkloadService.cs:605` | `tesPol` | literal `"neutral"` |
| `QaWorkloadService.cs:609` | `runsPol` | literal `"neutral"` |
| `QaWorkloadService.cs:629` | `bugsPol` | literal `"neutral"` |

Note: QaWorkload's `BuildSingleTeamMetrics` passes the literal polarity **string** `"neutral"` / `"positive-up"` as the `polarity` **argument** to its `BuildMetricCard` (lines 459, 466, 474) — those ARE legitimate string-polarity inputs that flow through `MetricCardBuilder.ComputeDelta(delta, "neutral")`, which short-circuits to `"neutral"` correctly. That is the string-polarity path and is fine; only the five rows above (where `"neutral"` is the final stored field value, bool-path context) are DO-NOT-TOUCH.

---

## Polarity-path map per service (which `MetricCardBuilder` path applies)

| Service | Card shape | Polarity path used | Hard-coded `"neutral"` carve-out |
|---------|-----------|--------------------|-----------------------------------|
| DeveloperThroughput | none (flat delta bundle on `SprintBreakdown`) | bool: `Direction(decimal)` + `Polarity(decimal, positiveUp)` | yes — line 153 |
| DeveloperProgress | none | **no shared kernel** — `Direction` is a domain string enum (`"new-stall"`/`"worsening"`/…), keep verbatim | n/a |
| DeveloperDetail | none | **no shared kernel** — no delta helpers at all | n/a |
| DeveloperQuality | sparkline (`SparklinePoint`) on breakdown; deltas flat | bool: `Direction(decimal)` + `Polarity(decimal, true)` | yes — line 134 |
| EpicProgress | none | **no shared kernel** — no delta helpers | n/a |
| QaWorkload | sparkline-bearing `MetricCard` (team metrics) + flat delta bundle (per-dev) | string path for cards (`ComputeDelta`); bool path for the per-dev bundle | yes — lines 605/609/629 |
| QaTrends | none | **no shared kernel** — no delta helpers | n/a |

**Consequence:** Only **DeveloperThroughput, DeveloperQuality, QaWorkload** delete private `DeltaDirection`/`DeltaPolarity`/`BuildMetricCard` helpers and call `MetricCardBuilder`. **DeveloperProgress, DeveloperDetail, EpicProgress, QaTrends** carry NO direction/polarity kernel — their migration is a pure relocate + rename + clean-break-return-type change, with their existing private helpers (`CountBusinessDays`, `ResolveStartIndex`, `ComputeMedian`, `ComputePearsonR`, etc.) moving with them unchanged.

---

## Files to Create (domain layer — 7 calculators + their result files)

| New file | Source (current API service file) |
|----------|------------------------------------|
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperThroughputResult.cs` | result records from `DeveloperThroughputService.cs` lines 4–42 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperThroughputCalculator.cs` | `DeveloperThroughputService.cs` lines 46–332 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperProgressResult.cs` | result records from `DeveloperProgressService.cs` lines 4–47 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperProgressAnalyzer.cs` | `DeveloperProgressService.cs` lines 51–380 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperDetailResult.cs` | result records from `DeveloperDetailService.cs` lines 4–64 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperDetailCalculator.cs` | `DeveloperDetailService.cs` lines 66–514 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperQualityResult.cs` | result records from `DeveloperQualityService.cs` lines 6–43 |
| `src/Services/Fokus/Fokus.Domain/Analytics/DeveloperQualityScorer.cs` | `DeveloperQualityService.cs` lines 47–506 |
| `src/Services/Fokus/Fokus.Domain/Analytics/EpicProgressResult.cs` | result + input records from `EpicProgressService.cs` lines 4–77 (incl. `EpicQaData` input type) |
| `src/Services/Fokus/Fokus.Domain/Analytics/EpicProgressAnalyzer.cs` | `EpicProgressService.cs` lines 81–605 |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaWorkloadResult.cs` | result records from `QaWorkloadService.cs` lines 6–101 |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaWorkloadAnalyzer.cs` | `QaWorkloadService.cs` lines 105–718 (+ the `NullableStringComparer` at lines 720–731) |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaTrendsResult.cs` | result records from `QaTrendsService.cs` lines 4–37 |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaTrendsAnalyzer.cs` | `QaTrendsService.cs` lines 41–372 |

## Files to Create (API layer — shared wire file + per-endpoint wire files)

| New file | Purpose |
|----------|---------|
| `src/Services/Fokus/Fokus.API/Features/Analytics/SharedAnalyticsWire.cs` | **Option B (Q1)** — the cross-service shared wire records (see Step 1) |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputResponse.cs` | wire records + Mapster `IRegister` for throughput |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressResponse.cs` | wire records + Mapster `IRegister` for progress |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailResponse.cs` | wire records + Mapster `IRegister` for detail |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressResponse.cs` | wire records + Mapster `IRegister` for epic progress |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsResponse.cs` | wire records + Mapster `IRegister` for QA trends |

> DeveloperQuality and QaWorkload already have wire DTOs in their `*Query.cs` files and hand-written mappers (Q3) — they get **no** new `*Response.cs` and **no** Mapster `IRegister`. Their existing mappers are re-pointed at the moved domain types in Steps 5 and 7.

## Files to Modify

| File | Change |
|------|--------|
| 7× `Get{Feature}Endpoint.cs` | swap service → calculator injection; map domain result → wire (Mapster for 5; existing hand-mapper re-pointed for Quality + Workload) |
| `GetDeveloperQuality/GetDeveloperQualityQuery.cs` | unchanged wire DTOs — verify only (Step 5) |
| `GetQaWorkload/GetQaWorkloadQuery.cs` | unchanged wire DTOs — verify only (Step 7) |
| `GetQaTrends/GetQaTrendsEndpoint.cs` | also keep the existing `BugRatioCalculator` injection (3c-B) — see Step 8 |
| `src/Services/Fokus/Fokus.API/DependencyInjection.cs` | swap 7 service registrations → 7 calculator registrations (Step 9) |

## Files to Delete (Step 10, after grep)

`DeveloperThroughputService.cs`, `DeveloperProgressService.cs`, `DeveloperDetailService.cs`, `DeveloperQualityService.cs`, `EpicProgressService.cs`, `QaWorkloadService.cs`, `QaTrendsService.cs` — all under `Fokus.API/Features/Analytics/`.

---

## Ordering

Step 1 (shared wire file) lands first because Steps 2, 3, 4, 7 reference its records. Each calculator migration (Steps 2–8) is order-independent of the others. Then DI swap (Step 9), delete (Step 10), verify (Step 11).

Execution order: **1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11**

---

## Step 1 — Create the shared API-layer wire file (Option B)

**Skill:** None (cross-feature shared wire-record file — Option B is an explicit user decision; no skill covers a deliberately-shared wire file)
**File:** Create `src/Services/Fokus/Fokus.API/Features/Analytics/SharedAnalyticsWire.cs`

Per Q1 → Option B, the records currently defined inside `DeveloperThroughputService.cs` and `DeveloperProgressService.cs` that are **consumed by more than one endpoint** move here as **wire** records, in namespace `Fokus.API.Features.Analytics` (the shared analytics namespace — both consuming endpoint namespaces already see it via the existing folder-level `namespace Fokus.API.Features.Analytics;` on the service files and the `using` graph). Keep each record's **exact name and field shape** (wire stability).

Move these records verbatim (names/fields unchanged):
- `SprintSummaryItem(int Id, string Name, DateTime StartDate, DateTime EndDate)` — currently `DeveloperThroughputService.cs:6`. Consumers: DeveloperThroughput wire + QaWorkload wire (`MapSprintInfo`).
- `DeveloperProgressSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)` — currently `DeveloperProgressService.cs:6`. Consumers: DeveloperProgress wire + DeveloperDetail wire.
- `CompletedTicketEntry(string Key, string Summary, decimal? StoryPoints, string IssueType)` — currently `DeveloperProgressService.cs:17`. Consumers: DeveloperProgress + DeveloperDetail.
- `DayBreakdownEntry(int Day, DateTime Date, List<CompletedTicketEntry> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp)` — currently `DeveloperProgressService.cs:19`. Consumers: DeveloperProgress + DeveloperDetail.
- `StalledTicketEntry(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition)` — currently `DeveloperProgressService.cs:21`. Consumer today: DeveloperProgress only — but it is structurally part of the DeveloperProgress current-sprint cluster shared with Detail's `CurrentSprintDetail` flow; **include it here** so the Progress/Detail structural set lives together (DeveloperDetail's `TicketDetailEntry` is distinct and stays in Detail's own wire file).

**Domain-side counterpart:** the domain calculators must NOT return these wire types (ADR-006/010). Each calculator returns a **domain** record carrying the same fields under a domain name; the endpoint maps domain → these shared wire records. The domain counterparts are defined in the per-calculator `*Result.cs` (Steps 2–4) as:

| Shared wire record (this file) | Domain counterpart (in `*Result.cs`) |
|-------------------------------|----------------------------------------|
| `SprintSummaryItem` | `SprintSummaryItemData` |
| `DeveloperProgressSprintInfo` | `DeveloperProgressSprintData` |
| `CompletedTicketEntry` | `CompletedTicketData` |
| `DayBreakdownEntry` | `DayBreakdownData` |
| `StalledTicketEntry` | `StalledTicketData` |

> **Single-ownership rule (HIGH).** Each shared wire record is declared **exactly once** — here. After Steps 2–4 delete the old service files, grep must show zero other declarations (Step 11 check 6). Do NOT also declare them in any per-endpoint `*Response.cs`.

**Acceptance:** file compiles; 5 shared wire records present with byte-identical field shapes to their current definitions; namespace `Fokus.API.Features.Analytics`.

**Confidence:** high — verbatim record relocation into a shared file; the only judgment is which records are genuinely cross-consumed (enumerated above from grep).

---

## Step 2 — DeveloperThroughputCalculator + result + wire + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/DeveloperThroughputResult.cs`
- Create `Fokus.Domain/Analytics/DeveloperThroughputCalculator.cs`
- Create `Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputResponse.cs`
- Modify `Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs`

**Domain records** (`DeveloperThroughputResult.cs`, source `DeveloperThroughputService.cs` 4–42) — rename map:

| Old (wire) | New (domain) |
|-----|-----------|
| `SprintSummaryItem` | `SprintSummaryItemData` (the shared-wire counterpart from Step 1) |
| `SprintBreakdown` | `SprintBreakdownData` |
| `DeveloperThroughputEntry` | `DeveloperThroughputEntryData` |
| `DeveloperThroughputResponse` | `DeveloperThroughputResultData` |

`SprintBreakdown` is a **flat delta bundle** (24 fields incl. 5 direction + 5 polarity strings) — NOT a metric card. Field shapes unchanged on the domain side.

**Calculator** (`ComputeThroughput`, source 46–332): returns `DeveloperThroughputResultData`. Delete the private `DeltaDirection(decimal)`/`DeltaDirection(int)`/`DeltaPolarity(decimal,bool)`/`DeltaPolarity(int,bool)` (lines 317–331) → call `MetricCardBuilder.Direction(...)` / `MetricCardBuilder.Polarity(..., positiveUp)`. **Carve-out:** `spAssignedDeltaPolarity` at line 153 is the literal `"neutral"` — keep verbatim, do NOT route through `MetricCardBuilder.Polarity`. All other private helpers (`ComputeMetrics`, `IsBug`, `FilterDevelopers`, `GetDeveloperMemberships`, `GetCapacity`, `ComputeRollingAverage`) move unchanged.

**Wire file** (`GetDeveloperThroughputResponse.cs`, namespace `…GetDeveloperThroughput`, `using Mapster;` + `using Fokus.Domain.Analytics;`): declare wire `SprintBreakdown`, `DeveloperThroughputEntry`, `DeveloperThroughputResponse` (exact current shapes). Do **NOT** redeclare `SprintSummaryItem` — it lives in `SharedAnalyticsWire.cs` (Step 1). Add an `IRegister` mapping `DeveloperThroughputResultData → DeveloperThroughputResponse` (+ nested `SprintBreakdownData → SprintBreakdown`, `DeveloperThroughputEntryData → DeveloperThroughputEntry`, `SprintSummaryItemData → SprintSummaryItem`). Pattern: 3c-B `GetCarryOverResponse.cs` IRegister.

**Endpoint:** inject `DeveloperThroughputCalculator`; the **one** early empty return (`new DeveloperThroughputResponse([], [])`, line 23) stays a wire construction; map the computed domain result via `.Adapt<DeveloperThroughputResponse>()` before `Send.OkAsync`.

**Acceptance:** build green; `GET /api/analytics/developer-throughput` JSON byte-identical — in particular every `SpAssignedDeltaPolarity` is `"neutral"`.

**Confidence:** medium — flat delta bundle with int+decimal overloads and the line-153 literal carve-out.

---

## Step 3 — DeveloperProgressAnalyzer + result + wire + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/DeveloperProgressResult.cs`
- Create `Fokus.Domain/Analytics/DeveloperProgressAnalyzer.cs`
- Create `Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressResponse.cs`
- Modify `Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs`

**Domain records** (`DeveloperProgressResult.cs`, source 4–47) — rename map:

| Old (wire) | New (domain) |
|-----|-----------|
| `DeveloperProgressSprintInfo` | `DeveloperProgressSprintData` (shared-wire counterpart, Step 1) |
| `DeveloperProgressAlert` | `DeveloperProgressAlertData` |
| `CompletedTicketEntry` | `CompletedTicketData` (shared-wire counterpart, Step 1) |
| `DayBreakdownEntry` | `DayBreakdownData` (shared-wire counterpart, Step 1) |
| `StalledTicketEntry` | `StalledTicketData` (shared-wire counterpart, Step 1) |
| `DeveloperProgressEntry` | `DeveloperProgressEntryData` |
| `DeveloperProgressResponse` | `DeveloperProgressResultData` |

**No shared-kernel polarity here.** `DeveloperProgressAlert.Direction` is a domain string enum (`"new-stall"`, `"stall-resolved"`, `"worsening"`, `"improving"`, `"stable"`) computed inline — keep that derivation verbatim; it is NOT a `MetricCardBuilder` candidate. All private helpers (`BuildStalledTickets`, `GetCapacity`, `CountBusinessDays`) and the alert-sort `OrderBy` chain move unchanged.

**Note — these domain records are consumed by DeveloperDetail (Step 4).** `DeveloperProgressSprintData`, `DayBreakdownData`, `CompletedTicketData` are referenced by `DeveloperDetailResult.cs`'s `CurrentSprintDetailData`. Declare them here in `DeveloperProgressResult.cs`; Step 4 references them (both `*Result.cs` files are in the same `Fokus.Domain.Analytics` namespace, so no extra using).

**Wire file** (`GetDeveloperProgressResponse.cs`): declare wire `DeveloperProgressAlert`, `DeveloperProgressEntry`, `DeveloperProgressResponse` (exact current shapes). Do **NOT** redeclare `DeveloperProgressSprintInfo`/`CompletedTicketEntry`/`DayBreakdownEntry`/`StalledTicketEntry` — they live in `SharedAnalyticsWire.cs`. Add `IRegister` `DeveloperProgressResultData → DeveloperProgressResponse` (+ nested maps incl. the shared-wire counterparts).

**Endpoint:** inject `DeveloperProgressAnalyzer`; the **two** early empty returns (`new DeveloperProgressResponse(false, null, 0, 0, false, [], [])`, lines 25 and 33) stay as wire constructions; `.Adapt<DeveloperProgressResponse>()` on the computed result.

**Acceptance:** build green; `GET /api/analytics/developer-progress` JSON byte-identical incl. the alert `direction` string set and alert ordering.

**Confidence:** medium — domain `Direction` enum must be preserved verbatim (do not "kernel-ize" it); alert sort comparators must survive the relocation unchanged.

---

## Step 4 — DeveloperDetailCalculator + result + wire + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/DeveloperDetailResult.cs`
- Create `Fokus.Domain/Analytics/DeveloperDetailCalculator.cs`
- Create `Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailResponse.cs`
- Modify `Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs`

**Domain records** (`DeveloperDetailResult.cs`, source 4–64) — rename map:

| Old (wire) | New (domain) |
|-----|-----------|
| `DeveloperDetailInfo` | `DeveloperDetailInfoData` |
| `SprintTrendEntry` | `SprintTrendData` |
| `WorkAllocationSummary` | `WorkAllocationData` |
| `CurrentSprintDetail` | `CurrentSprintDetailData` (its `Sprint`, `DailyBreakdown` fields now typed `DeveloperProgressSprintData`, `List<DayBreakdownData>` — the Step 3 domain records) |
| `TicketDetailEntry` | `TicketDetailData` |
| `DeveloperDetailResponse` | `DeveloperDetailResultData` |

`CurrentSprintDetailData` references `DeveloperProgressSprintData`, `DayBreakdownData`, `CompletedTicketData` (from Step 3's `DeveloperProgressResult.cs`, same namespace). `TicketDetailEntry`/`TicketDetailData` is **Detail-only** — its wire form stays in this endpoint's `GetDeveloperDetailResponse.cs`, not the shared file.

**Calculator** (`ComputeDetail`, source 66–514): returns `DeveloperDetailResultData`. **No delta/polarity kernel** — no metric cards, no direction/polarity helpers. All private helpers (`ComputeCurrentSprintDetail`, `BuildTicketDetails`, `ComputeRollingAverage`, `IsBug`, `GetDeveloperMemberships`, `GetTransitionCompletedMemberships`, `GetCapacity`, `CountBusinessDays`) move unchanged. The `jiraInstanceUrl`/`bugRatioTarget` scalars stay value-typed parameters (intrinsic primitives — ADR-006 compliant).

**Wire file** (`GetDeveloperDetailResponse.cs`): declare wire `DeveloperDetailInfo`, `SprintTrendEntry`, `WorkAllocationSummary`, `CurrentSprintDetail`, `TicketDetailEntry`, `DeveloperDetailResponse` (exact current shapes; `CurrentSprintDetail.Sprint`/`.DailyBreakdown`/`.Tickets` reference the **shared** wire `DeveloperProgressSprintInfo`/`DayBreakdownEntry` + Detail-local `TicketDetailEntry`). Do **NOT** redeclare the shared records. Add `IRegister` `DeveloperDetailResultData → DeveloperDetailResponse` (+ nested maps).

**Endpoint:** inject `DeveloperDetailCalculator`; keep the `IOptions<JiraOptions> jiraOptions` injection and pass `jiraOptions.Value.InstanceUrl` + `settings.BugRatio.Target` as today; `.Adapt<DeveloperDetailResponse>()` on the result. The `Send.NotFoundAsync` early returns are unchanged.

**Acceptance:** build green; `GET /api/analytics/developer-detail/{accountId}` JSON byte-identical.

**Confidence:** medium — the cross-step domain dependency on Step 3's records (`DeveloperProgressSprintData`/`DayBreakdownData`/`CompletedTicketData`) is the main coupling; both files are same-namespace so it resolves, but the developer must land Step 3 first.

---

## Step 5 — DeveloperQualityScorer + result + re-point existing hand-mapper

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/DeveloperQualityResult.cs`
- Create `Fokus.Domain/Analytics/DeveloperQualityScorer.cs`
- Modify `Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs`

**Q3 — keep the existing hand-written mapper.** DeveloperQuality already has wire DTOs in `GetDeveloperQualityQuery.cs` (`DeveloperQualityResponse`, `DeveloperQualitySprintInfoResponse`, `DeveloperQualityEntryResponse`, `DeveloperQualitySprintBreakdownResponse`, `SparklinePointResponse`) and a hand-written `MapResponse` in the endpoint. **Leave the wire DTOs and `MapResponse` structure intact** — only move the result records to Domain and re-point `MapResponse`'s parameter type. No `*Response.cs` file, no Mapster `IRegister`.

**Domain records** (`DeveloperQualityResult.cs`, source `DeveloperQualityService.cs` 6–43) — rename map:

| Old (in service file) | New (domain) |
|-----|-----------|
| `DeveloperQualitySprintInfo` | `DeveloperQualitySprintData` |
| `DeveloperQualitySprintBreakdown` | `DeveloperQualitySprintBreakdownData` |
| `DeveloperQualityEntry` | `DeveloperQualityEntryData` |
| `DeveloperQualityResult` | `DeveloperQualityResultData` |

`DeveloperQualitySprintBreakdownData` carries `List<SparklinePoint>? CoverageSparkline`/`PassRateSparkline`. **`SparklinePoint` is the wire type in `GetSprintSummaryQuery.cs` (API layer) — a domain record must NOT reference it** (ADR-006).

**Decided (critic A2 — no question needed):** add a **DeveloperQuality-local domain sparkline point** `DeveloperQualitySparklinePointData(string SprintName, decimal Value)` in `DeveloperQualityResult.cs`. Do **NOT** reuse another calculator's domain sparkline type (3c-A `SprintSparklinePoint`/`QaSparklinePoint`) — the 3c-A pattern is each calculator owns its own domain sparkline point; cross-calculator reuse is forbidden (Step 11 check 10 guards this). The wire `SparklinePoint` and this domain point are field-identical (`SprintName`, `Value`), so the endpoint's hand-mapper just re-points its `p` projection at the domain type: `b.CoverageSparkline?.Select(p => new SparklinePointResponse {...})` is byte-unchanged.

**Scorer** (`ComputeDeveloperQuality`, source 47–506): returns `DeveloperQualityResultData`. Delete the private `DeltaDirection(decimal)`/`DeltaDirection(int)`/`DeltaPolarity(decimal,bool)` (lines 441–450) → `MetricCardBuilder.Direction(...)`/`MetricCardBuilder.Polarity(..., true)`. **Carve-out:** `bugsDeltaPol` at line 134 is the literal `"neutral"` — keep verbatim. Private `ComputeDevMetrics`, `GetActiveFeatureTicketKeysForDev`, `ComputePassRate`, `BuildTestsTesByTicket`, `BuildDevSparkline`, `ComputeBelowMedianStreak`, `ComputeMedian`, `FilterDevelopers`, `GetEffectiveSp` move unchanged. The local `IsStartedInSprint`/`GetStageIndex`/`ResolveStartIndex` private helpers (lines 463–505) move with the scorer (the scorer does NOT use `TransitionAttributionChecker` for these — keep its own copies to preserve behavior exactly; do not "consolidate" them onto the shared helper in this pass — that risks a behavior change and is out of scope).

**Endpoint:** inject `DeveloperQualityScorer`; re-point `MapResponse(DeveloperQualityResultData result)` and its `BuildDevSparkline` projection at the domain types; the early empty returns (`new DeveloperQualityResponse { HasQaData = false, ... }`) stay as wire constructions.

**Acceptance:** build green; `GET /api/analytics/developer-quality` JSON byte-identical; `BugsFoundDeltaPolarity` is `"neutral"` everywhere; no domain file references the API-layer `SparklinePoint`.

**Confidence:** medium — sparkline-point decision now resolved (DeveloperQuality-local domain point, A2); residual care is the line-134 carve-out and the local `ResolveStartIndex` duplication moving verbatim (not merged with `TransitionAttributionChecker`).

---

## Step 6 — EpicProgressAnalyzer + result + wire + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/EpicProgressResult.cs`
- Create `Fokus.Domain/Analytics/EpicProgressAnalyzer.cs`
- Create `Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressResponse.cs`
- Modify `Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs`

**Domain records** (`EpicProgressResult.cs`, source 4–77) — rename map:

| Old (wire/input) | New (domain) |
|-----|-----------|
| `EpicProgressSummaryMetrics` | `EpicProgressSummaryData` |
| `TestRunSummaryDto` | `TestRunSummaryData` |
| `EpicProgressTicketEntry` | `EpicProgressTicketData` |
| `EpicProgressEntry` | `EpicProgressEntryData` |
| `EpicProgressUnlinkedWork` | `EpicProgressUnlinkedData` |
| `EpicProgressResponse` | `EpicProgressResultData` |
| `EpicQaData` (input type) | `EpicQaData` — **stays this name**, moves to domain as the calculator's **input** record (it carries only primitive dictionaries `Dictionary<string,List<string>>` / `Dictionary<string,List<TestRun>>` — `TestRun` is a domain entity, ADR-006-safe). It is an input, not a response DTO. |

**No delta/polarity kernel.** No metric cards, no direction/polarity helpers. All private helpers (`GetEffectiveTicketSp`, `FilterTickets`, `FilterMemberships`, `ComputeEpicQaMetrics`, `BuildTicketEntry`) move unchanged. Uses `CompletionChecker`, `TransitionAttributionChecker`, `HealthScoreCalculator` (already domain).

**Wire file** (`GetEpicProgressResponse.cs`): declare wire `EpicProgressSummaryMetrics`, `TestRunSummaryDto`, `EpicProgressTicketEntry`, `EpicProgressEntry`, `EpicProgressUnlinkedWork`, `EpicProgressResponse` (exact current shapes). `EpicQaData` is **not** a wire type — it does not go in the wire file. Add `IRegister` `EpicProgressResultData → EpicProgressResponse` (+ nested maps).

**Endpoint:** inject `EpicProgressAnalyzer`; construct `EpicQaData` exactly as today (now the domain input type) before calling the analyzer; `.Adapt<EpicProgressResponse>()` on the result; early empty return stays a wire construction.

**Acceptance:** build green; `GET /api/analytics/epic-progress` JSON byte-identical.

**Confidence:** high — pure structural relocate; the only nuance is `EpicQaData` moving to domain as an input record (keeps its name).

---

## Step 7 — QaWorkloadAnalyzer + result + re-point existing hand-mapper

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/QaWorkloadResult.cs`
- Create `Fokus.Domain/Analytics/QaWorkloadAnalyzer.cs`
- Modify `Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs`

**Q3 — keep the existing hand-written mappers.** QaWorkload already has wire DTOs in `GetQaWorkloadQuery.cs` (`QaWorkloadResponseDto`, `QaWorkloadMultiSprintDto`, `QaWorkloadSingleSprintDto`, `QaWorkloadTeamMetricsDto`, `QaWorkloadSingleTeamMetricsDto`, `QaWorkloadEntryDto`, `QaWorkloadSingleEntryDto`, `QaWorkloadSprintInfoDto`, `WorkloadAlertDto`, `QaWorkloadMetricCardDto`, `SparklinePointDto`) and hand-written `MapMultiResponse`/`MapSingleResponse`/`MapEntry`/`MapSingleEntry`/`MapMetricCard`/`MapSprintInfo`/`MapAlert`. **Leave the wire DTOs and mappers intact** — only move result records to Domain and re-point. No `*Response.cs`, no Mapster `IRegister`.

**Domain records** (`QaWorkloadResult.cs`, source 6–101) — rename map:

| Old (in service file) | New (domain) |
|-----|-----------|
| `QaWorkloadTeamMetrics` | `QaWorkloadTeamMetricsData` |
| `QaWorkloadSprintBreakdown` | `QaWorkloadSprintBreakdownData` |
| `WorkloadAlert` | `WorkloadAlertData` |
| `QaWorkloadEntry` | `QaWorkloadEntryData` |
| `QaWorkloadMultiSprintResponse` | `QaWorkloadMultiSprintData` |
| `QaWorkloadSingleTeamMetrics` | `QaWorkloadSingleTeamMetricsData` (its three card fields now typed with the **domain** sparkline card — see below) |
| `QaWorkloadSingleEntry` | `QaWorkloadSingleEntryData` |
| `QaWorkloadSingleSprintResponse` | `QaWorkloadSingleSprintData` |
| `QaWorkloadResponse` (the `{HasQaData, Mode, Multi?, Single?}` envelope, service file line 97) | **DEAD code — deleted with the service file (NOT moved, NOT mirrored to domain).** The endpoint already builds the wire envelope via `QaWorkloadResponseDto` (in `GetQaWorkloadQuery.cs`); this `QaWorkloadResponse` record is unused today and simply disappears when `QaWorkloadService.cs` is deleted in Step 10. Do NOT recreate it anywhere. The calculator returns `QaWorkloadMultiSprintData` + `QaWorkloadSingleSprintData` separately (as `ComputeMultiSprint`/`ComputeSingleSprint` already do). |

**Sparkline-bearing card — `MetricCard` is a wire type (ADR-006).** `QaWorkloadSingleTeamMetrics` today uses `MetricCard` (from `GetSprintSummaryQuery.cs`, API layer) with `Sparkline` + `Rag`. The domain `QaWorkloadSingleTeamMetricsData` must use a **domain** card type, not the wire `MetricCard`.

**Decided (critic A1 — no question needed):** add a **QaWorkload-local domain sparkline card** in `QaWorkloadResult.cs` carrying the 6 base card fields + `Rag` + a `List<QaWorkloadSparklinePointData>` (where `QaWorkloadSparklinePointData(string SprintName, decimal Value)` is also declared locally in `QaWorkloadResult.cs`). The wire `SparklinePoint` and this domain point are field-identical, confirmed by the critic. **Do NOT reuse another calculator's domain types** (3c-A `SprintMetricCardData`/`QaMetricCardData`, `SprintSparklinePoint`/`QaSparklinePoint`) — per the 3c-A pattern each calculator owns its own domain sparkline point/card; cross-calculator reuse is forbidden (Step 11 check 10 guards this). The endpoint's existing `MapMetricCard(MetricCard card)` re-points its parameter to this QaWorkload-local domain card and projects to `QaWorkloadMetricCardDto` (unchanged wire output).

**Analyzer** (source 105–718 + `NullableStringComparer` 720–731): returns the two domain shapes. The internal `PersonMetrics` sealed record (lines 111–126) stays private. Delete the private `BuildMetricCard` (676–707) → build the domain sparkline card via `MetricCardBuilder.ComputeDelta(delta, polarity)` for direction/polarity + the existing sparkline/Rag wiring (this is the string-polarity path; its `"neutral"`/`"positive-up"` polarity **arguments** at lines 459/466/474 are legitimate string inputs — NOT the DO-NOT-TOUCH literals). Delete private `DeltaDirection(decimal)`/`DeltaPolarity(decimal,bool)` (711–717) → `MetricCardBuilder.Direction`/`Polarity`. **Carve-out:** `tesPol` (605), `runsPol` (609), `bugsPol` (629) are literal `"neutral"` field values — keep verbatim. `NullableStringComparer` moves to the domain (it is a pure `IEqualityComparer<string?>` with no API dependency) alongside the analyzer or as a sibling internal class in `QaWorkloadResult.cs`. Static helper `GetAttributedAccountIdStatic`, `BuildSprintPersonMetrics`, `FilterPersonMapBySubTeam`, `BuildTeamMetrics`, `BuildTeamSparkline`, `EvaluateWorkloadAlert`, `BuildDeveloperEntries`, `BuildSingleDeveloperEntries`, `BuildSingleTeamMetrics` move unchanged (with card return type swapped to the domain card).

**Endpoint:** inject `QaWorkloadAnalyzer`; re-point `MapMultiResponse(QaWorkloadMultiSprintData)`, `MapSingleResponse(QaWorkloadSingleSprintData)`, `MapMetricCard(<domain card>)`, `MapEntry`/`MapSingleEntry`/`MapSprintInfo`/`MapAlert` at the moved domain types; keep the mode-branching control flow (`HandleSingleSprintAsync`/`HandleMultiSprintAsync`) and all `QaWorkloadResponseDto` envelope construction exactly as today.

**Acceptance:** build green; `GET /api/analytics/qa-workload` JSON byte-identical in both modes; `TesOwnedPolarity`/`RunsCompletedPolarity`/`BugsFoundPolarity` are `"neutral"` per the carve-out; no domain file references the API `MetricCard`/`SparklinePoint`.

**Confidence:** medium — most complex service, but the two open decisions are now resolved (A1: QaWorkload-local domain sparkline card; A3: dead `QaWorkloadResponse` is deleted not moved). Residual care: `NullableStringComparer` relocation, the 3 carve-out literals (605/609/629), and the string-vs-bool polarity split (string path for the team-metric cards, bool path for the per-dev bundle) all in one file.

---

## Step 8 — QaTrendsAnalyzer + result + wire + endpoint (preserve BugRatioCalculator injection)

**Skill:** `analytics-computation-service`
**Files:**
- Create `Fokus.Domain/Analytics/QaTrendsResult.cs`
- Create `Fokus.Domain/Analytics/QaTrendsAnalyzer.cs`
- Create `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsResponse.cs`
- Modify `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs`

**Domain records** (`QaTrendsResult.cs`, source 4–37) — rename map:

| Old (wire) | New (domain) |
|-----|-----------|
| `QaTrendsSprintInfo` | `QaTrendsSprintData` |
| `QualityTrendEntry` | `QualityTrendData` |
| `TestingVolumeEntry` | `TestingVolumeData` |
| `DefectCorrelationDataPoint` | `DefectCorrelationPointData` |
| `DefectCorrelationResult` | `DefectCorrelationData` |
| `QaTrendsResponse` | `QaTrendsResultData` |

**No delta/polarity kernel.** No metric cards. All private helpers (`ComputeDefectCorrelation`, `ComputePearsonR`, `GetActiveFeatureTicketKeys`, `IsStartedInSprint`, `GetStageIndex`, `GetEffectiveSp`, `BuildTestsTesByTicket`, `BuildRunsByTe`, `ComputeCoverageRate`, `ComputeExecutionRate`, `ComputePassRate`, `FilterMemberships`, `ResolveStartIndex`) move unchanged. The local `ResolveStartIndex` (355–371) moves verbatim — do NOT consolidate onto `TransitionAttributionChecker` (out of scope; behavior-parity risk).

**Wire file** (`GetQaTrendsResponse.cs`): declare wire `QaTrendsSprintInfo`, `QualityTrendEntry`, `TestingVolumeEntry`, `DefectCorrelationDataPoint`, `DefectCorrelationResult`, `QaTrendsResponse` (exact current shapes). Add `IRegister` `QaTrendsResultData → QaTrendsResponse` (+ nested maps).

**Endpoint — preserve the existing `BugRatioCalculator` injection (3c-B).** `GetQaTrendsEndpoint` injects BOTH `BugRatioCalculator` (3c-B domain calculator) and `QaTrendsService`. Swap only `QaTrendsService → QaTrendsAnalyzer`; **keep `BugRatioCalculator`**. The endpoint reads `bugRatioResult.TeamMetrics.PerSprintTrend` (domain `*Data` from 3c-B) to build `bugRatioBySprintId` — that wiring is unchanged. `.Adapt<QaTrendsResponse>()` on the QaTrends domain result; early empty returns stay wire constructions.

**Acceptance:** build green; `GET /api/analytics/qa-trends` JSON byte-identical; endpoint still injects `BugRatioCalculator` and `QaTrendsAnalyzer`; Pearson-r N≥6 gating preserved.

**Confidence:** medium — the dual-injection (BugRatioCalculator stays, QaTrends swaps) is the one place a careless "swap the service" edit could drop the 3c-B dependency.

---

## Step 9 — Update DI registration

**Skill:** None (mechanical registration swap)
**File:** Modify `src/Services/Fokus/Fokus.API/DependencyInjection.cs`

Swap the 7 service registrations (current lines 45, 49, 52, 53, 54, 55, 56) for the 7 calculator registrations, in the same block:

| Remove | Add |
|--------|-----|
| `AddScoped<DeveloperThroughputService>()` | `AddScoped<DeveloperThroughputCalculator>()` |
| `AddScoped<DeveloperProgressService>()` | `AddScoped<DeveloperProgressAnalyzer>()` |
| `AddScoped<DeveloperDetailService>()` | `AddScoped<DeveloperDetailCalculator>()` |
| `AddScoped<DeveloperQualityService>()` | `AddScoped<DeveloperQualityScorer>()` |
| `AddScoped<EpicProgressService>()` | `AddScoped<EpicProgressAnalyzer>()` |
| `AddScoped<QaWorkloadService>()` | `AddScoped<QaWorkloadAnalyzer>()` |
| `AddScoped<QaTrendsService>()` | `AddScoped<QaTrendsAnalyzer>()` |

`using Fokus.Domain.Analytics;` already present (line 7). The 3c-A/3c-B calculator registrations on adjacent lines stay untouched.

**Acceptance:** 7 calculators registered; 0 of the 7 old services referenced; build green.

---

## Step 10 — Delete the 7 old API-layer service files (grep before delete)

**Skill:** None
**Verification sub-step (hard, numbered — do BEFORE deleting):**
1. Grep each old class name across the **whole solution** — required result **zero hits** except the file being deleted:
   `DeveloperThroughputService`, `DeveloperProgressService`, `DeveloperDetailService`, `DeveloperQualityService`, `EpicProgressService`, `QaWorkloadService`, `QaTrendsService`.
2. Grep the relocated shared-wire record names to confirm **single ownership** (declared only in `SharedAnalyticsWire.cs`, used elsewhere):
   `record SprintSummaryItem`, `record DeveloperProgressSprintInfo`, `record CompletedTicketEntry`, `record DayBreakdownEntry`, `record StalledTicketEntry` — exactly **one definition each**, in `SharedAnalyticsWire.cs`.

**Delete:** `DeveloperThroughputService.cs`, `DeveloperProgressService.cs`, `DeveloperDetailService.cs`, `DeveloperQualityService.cs`, `EpicProgressService.cs`, `QaWorkloadService.cs`, `QaTrendsService.cs` (all under `Fokus.API/Features/Analytics/`).

**Acceptance:** build green after deletion; zero compilation errors.

---

## Step 11 — Verification sweep

**Skill:** None

1. `dotnet build SprintRituals.slnx` — clean (0 errors; no new warnings beyond the pre-existing 4–6).
2. Grep `DeveloperThroughputService|DeveloperProgressService|DeveloperDetailService|DeveloperQualityService|EpicProgressService|QaWorkloadService|QaTrendsService` — zero hits.
3. Grep the 7 new domain class names in `Fokus.Domain/Analytics/` — 7 present; in `DependencyInjection.cs` — 7 registered (with the exact Q2 names).
4. Grep `: Endpoint<` in the 7 endpoints — each still declares its **wire** response type as the generic response param (byte-stability of the public contract).
5. Grep for any **domain** file under `Fokus.Domain/Analytics/` referencing API wire types — `MetricCard\b` (the sparkline wire card), `SparklinePoint\b` (API wire), `GetSprintSummary`, `Fokus.API` — **zero hits** (ADR-006: no API/wire type in domain).
6. Single-ownership of shared wire records (Step 10 sub-step 2 re-run): each of the 5 relocated records has exactly one `record` definition, in `SharedAnalyticsWire.cs`.
7. Carve-out literals intact: grep `"neutral"` in the 3 calculators that have it (Throughput, Quality, Workload) and confirm the 5 DO-NOT-TOUCH field assignments still read literal `"neutral"` (not `MetricCardBuilder.Polarity(...)`).
8. `BugRatioCalculator` still injected in `GetQaTrendsEndpoint` (Step 8).
9. **No cross-calculator sparkline-type reuse (critic N1 — guards A1/A2).** Grep `DeveloperQualityResult.cs` and `QaWorkloadResult.cs` — neither may reference `SprintSparklinePoint`, `QaSparklinePoint`, `SprintMetricCardData`, or `QaMetricCardData` (the 3c-A calculator-owned types). Each must use its own local domain sparkline point/card (`DeveloperQualitySparklinePointData`, `QaWorkloadSparklinePointData`). Zero hits required.
10. **Manual smoke test** — all 7 endpoints return HTTP 200 with byte-identical JSON shapes vs. pre-change. Single-sprint AND multi-sprint modes for the mode-bearing endpoints (developer-throughput, developer-quality, qa-workload). Spot-check: a developer with a non-zero `spAssignedDelta` still shows `spAssignedDeltaPolarity: "neutral"`.

**Acceptance:** all checks pass; no behavioral change; 7 services migrated to domain; no API/wire type leaked into the domain; no cross-calculator domain sparkline reuse.

---

## Skill Mapping

| Step | Skill | Disposition | Note |
|------|-------|-------------|------|
| 1 — Shared wire file (Option B) | None | None | Deliberately-shared cross-feature wire file (user decision Q1→B); no skill covers a shared wire-record file. Gap candidate: "shared cross-slice wire types" guidance. |
| 2 — DeveloperThroughputCalculator | `analytics-computation-service` | Follow | flat delta bundle; bool polarity path + line-153 carve-out |
| 3 — DeveloperProgressAnalyzer | `analytics-computation-service` | Follow | no kernel; domain `Direction` enum preserved verbatim |
| 4 — DeveloperDetailCalculator | `analytics-computation-service` | Follow | no kernel; consumes Step-3 domain records |
| 5 — DeveloperQualityScorer | `analytics-computation-service` | Follow | keep hand-mapper (Q3); domain sparkline-point decision; line-134 carve-out |
| 6 — EpicProgressAnalyzer | `analytics-computation-service` | Follow | no kernel; `EpicQaData` moves to domain as input record |
| 7 — QaWorkloadAnalyzer | `analytics-computation-service` | Follow | keep hand-mappers (Q3); sparkline-bearing domain card; 3 carve-outs; wire-only envelope |
| 8 — QaTrendsAnalyzer | `analytics-computation-service` | Follow | no kernel; preserve BugRatioCalculator injection |
| 9 — DI swap | None | None | mechanical registration swap |
| 10 — Delete + grep | None | None | grep-before-delete + single-ownership check |
| 11 — Verification | None | None | build + grep + smoke test |

---

## Self-check baseline (pre-critic)

Checked against ADR-005/006/010 + the 3c-A/3c-B clean-break precedent + the three user decisions:

- **ADR-005:** All 7 computations move to `Fokus.Domain/Analytics/` (Steps 2–8). Calculators return domain `*Result`/`*Data`.
- **ADR-006:** No API/wire DTO enters the domain. The two sparkline-bearing services (Quality, Workload) explicitly use a **domain** sparkline point, never the API `SparklinePoint`/`MetricCard` (Steps 5/7 + Step 11 check 5). Inputs are domain entities/VOs/primitives (incl. `EpicQaData`, which carries only primitive dicts + the `TestRun` domain entity).
- **ADR-010:** Endpoints own their wire contracts; 5 use Mapster `IRegister`, 2 keep hand-written boundary mappers (Q3). The QaWorkload `{HasQaData,Mode,Multi?,Single?}` envelope is endpoint-assembled, never in the domain.
- **Q1 → Option B:** shared structural wire records live in one `SharedAnalyticsWire.cs` (Step 1); single-ownership enforced by Step 10/11 grep.
- **Q2 → meaningful suffix:** the 7 names are assigned as binding contracts (Throughput→Calculator, Progress→Analyzer, Detail→Calculator, Quality→Scorer, Epic→Analyzer, Workload→Analyzer, Trends→Analyzer).
- **Q3 → keep hand-mappers** for Quality + Workload.
- **Wire stability:** every wire record keeps its exact name/shape; 7 HTTP contracts byte-identical; no gRPC touched.
- **Behavior-parity guards:** local `ResolveStartIndex`/`IsStartedInSprint` duplicates move verbatim (NOT merged onto `TransitionAttributionChecker`) to avoid silent behavior drift; alert direction enums and sort comparators preserved.

**Risks flagged for the critic:**
1. **QaWorkload sparkline-bearing domain card (Step 7)** — does a reusable 3c-A sparkline domain card exist, or must a new domain card be added? If the 3c-A sparkline element type differs, a question to the user/architect may be needed. This is the lowest-confidence step.
2. **DeveloperQuality domain sparkline point (Step 5)** — same reuse-vs-add decision, smaller blast radius.
3. **Cross-step domain coupling (Steps 3→4)** — DeveloperDetail's domain records reference DeveloperProgress's; must land Step 3 first.
4. **5 hard-coded `"neutral"` carve-outs** — a blanket polarity-kernel replace corrupts them; enumerated with file:line.
5. **Step count = 11 (>10).** Splitting considered: Step 1 (shared wire) is consumed by Steps 2/3/4/7, and Steps 9–10 depend on all migrations — a split would put the shared wire file or the deletes in a separate run from their consumers/dependencies, producing a **non-building intermediate state** (same rationale as 3c-B). **Kept whole.**

## Plan Review

### Critic Mode-2 review (code-grounded) — disposition

**Critic verdict: APPROVE** — 1 LOW + 4 advisories, no CRITICAL/HIGH/MEDIUM. All three locked user decisions (Q1→B, Q2→meaningful suffixes, Q3→keep hand-mappers) confirmed faithful. Findings: `docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/critic-review.md`. All folded:

| # | Sev | Finding | Disposition — folded into |
|---|-----|---------|----------------------------|
| LOW-1 | LOW | Early-return counts wrong: Throughput has ONE empty return (line 23) not two; Progress has TWO (lines 25, 33) not three (Quality=3, Epic=1 were correct) | **Fixed** — Step 2 now says "one early empty return (line 23)"; Step 3 says "two early empty returns (lines 25 and 33)" |
| A1 | advisory | Risk #1 (QaWorkload sparkline-bearing domain card) — wire `SparklinePoint` and both domain points are field-identical; 3c-A pattern = each calculator owns its own domain point | **Resolved (decided, no question)** — Step 7 now mandates a QaWorkload-local domain sparkline card + `QaWorkloadSparklinePointData` in `QaWorkloadResult.cs`, mirroring 3c-A; cross-calculator reuse forbidden. Confidence raised low → medium |
| A2 | advisory | Risk #2 (DeveloperQuality sparkline point) — same resolution | **Resolved (decided, no question)** — Step 5 now mandates a DeveloperQuality-local `DeveloperQualitySparklinePointData` in `DeveloperQualityResult.cs`; hand-mapper re-points `p` |
| A3 | advisory | Step 7 wording: `QaWorkloadResponse` (service line 97) is DEAD code today — endpoint builds `QaWorkloadResponseDto` directly | **Fixed** — Step 7 rename table now states `QaWorkloadResponse` is deleted with the service file (not moved, not mirrored); action unchanged (don't mirror envelope to domain) |
| N1 | advisory | Add a Step 11 grep asserting `QaWorkloadResult.cs`/`DeveloperQualityResult.cs` do NOT reference `SprintSparklinePoint`/`QaSparklinePoint`/`SprintMetricCardData`/`QaMetricCardData` (guards A1/A2) | **Added** — Step 11 check 9 (new); smoke test renumbered to check 10 |
| A4 | advisory | `StalledTicketEntry` in the shared file — is it genuinely cross-consumed? | **Accepted for transparency (no change)** — its inclusion is a faithful execution of the Q1 answer, which explicitly enumerated it; Step 1 documents that it's a structural-cluster decision, not a usage-driven one |

No finding required re-architecture; the critic confirmed all three decision checks. **Plan approved.**

### Self-check baseline (pre-critic) — superseded items
Risks #1 and #2 in the self-check above ("may need a question") are now **resolved by critic A1/A2** — no question needed; the per-calculator-local domain sparkline type is the decided approach. Risks #3 (Step 3→4 coupling), #4 (carve-outs), #5 (step count) stand.
