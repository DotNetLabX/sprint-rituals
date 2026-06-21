# Sprint Test Coverage

**Feature Spec:** `docs/specs/F26-SprintTestCoverage/definition/spec.md`

## Context

Fokus v1 surfaces delivery health metrics (completion, disruption, carry-over) on the Dashboard. With F25 complete, the database now contains Xray test execution data (TestExecution, TestExecutionLink, TestRun entities) synced alongside sprint data. F26 surfaces sprint-level QA metrics on the Dashboard -- Coverage Rate, Execution Rate, Pass Rate -- and integrates a Quality sub-score into the composite health score. This answers "How well tested is this sprint?" using the data foundation F25 laid.

**Services impacted:** Fokus (single service). No new modules or external integrations -- F26 reads from entities and settings already in the database.

**Route convention note:** The three new QA endpoints use `/api/sprints/{sprintId}/qa-metrics/*` rather than the existing `/api/analytics/*` pattern. This is per spec design -- the QA metrics are scoped to a specific sprint by ID in the route (not query parameter), which differs from the analytics pattern where sprintId is a query parameter. The spec defines this route shape explicitly.

## Scope

**In scope:**
- Three new QA metric endpoints: GET /api/sprints/{sprintId}/qa-metrics, /qa-metrics/untested, /qa-metrics/failing
- QaMetricsService for computing coverage rate, execution rate, pass rate, bugs found, quality sub-score
- AppSettings extensions: QA health thresholds, quality health weight, quality sub-score weights
- SaveHealthConfig endpoint extension for QA fields + validation
- GetSettings response extension for QA settings fields
- Health score integration: Quality as fourth sub-score with additive weight model
- HealthScoreResult extension: qualitySubScore, qualityRag, qualityBreakdown
- Ticket entity extension: ParentTicketKey for sub-task inheritance (BR9)
- TestExecutionRepository extension: sprint-scoped TE query method
- Frontend: QA section on Dashboard, health score badge extension, settings UI Quality sub-group
- Help tooltips on all QA UI elements
- Empty state handling (Xray disabled, not synced, zero TEs)

**Out of scope:**
- Per-developer QA breakdown (F28)
- QA workload distribution (F29)
- Test execution timeline (F30)
- Epic-level test health (F31)
- Cross-sprint QA trends (F27)
- Test Set-based metrics
- Dedicated QA sidebar page

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | AppSettings property extensions (QA thresholds, weights), new value objects QaHealthThresholdConfig, QualitySubScoreWeightConfig | |
| 2 | persistence-patterns | Follow | AppSettings EF config update for new owned JSON columns, migration | |
| 3 | domain-patterns | Follow | Ticket entity gains ParentTicketKey (string?), Jira sync mapper update | |
| 4 | persistence-patterns | Follow | Ticket EF config update for ParentTicketKey, migration | |
| 5 | (none) | -- | TestExecutionRepository: sprint-scoped TE query joining through TestExecutionLink -> SprintMembership | Log: no skill for complex cross-entity query methods |
| 6 | (none) | -- | QaMetricsService: metric computation (coverage rate, execution rate, pass rate, bugs found, quality sub-score, delta, sparkline) | Log: no skill for metric computation patterns |
| 7 | create-feature | Follow | GetQaMetrics endpoint (GET /api/sprints/{sprintId}/qa-metrics), FastEndpoints | |
| 8 | create-feature | Follow | GetUntestedTickets endpoint (GET /api/sprints/{sprintId}/qa-metrics/untested), FastEndpoints | |
| 9 | create-feature | Follow | GetFailingTickets endpoint (GET /api/sprints/{sprintId}/qa-metrics/failing), FastEndpoints | |
| 10 | (none) | -- | Extend SprintSummaryService health score computation: add Quality fourth sub-score with additive weight model, extend HealthScoreResult record | Log: no skill for extending existing computation service |
| 11 | create-feature | Follow | Extend SaveHealthConfig endpoint: add QA threshold and weight fields + validation | |
| 12 | create-feature | Follow | Extend GetSettings endpoint: add QA settings fields to response | |
| 13 | vue-patterns, pinia-patterns | Follow | Frontend QA metrics section on Dashboard, health score badge extension | |
| 14 | vue-patterns, pinia-patterns | Follow | Frontend Settings UI Quality sub-group | |

## Domain Model Changes

### New Value Objects

**QaHealthThresholdConfig** -- `ValueObject` (owned by AppSettings, stored as JSON column)
- CoverageGreen (decimal, default 80)
- CoverageAmber (decimal, default 50)
- ExecutionGreen (decimal, default 80)
- ExecutionAmber (decimal, default 50)
- PassRateGreen (decimal, default 90)
- PassRateAmber (decimal, default 70)

**QualitySubScoreWeightConfig** -- `ValueObject` (owned by AppSettings, stored as JSON column)
- CoverageWeight (int, default 50)
- PassRateWeight (int, default 50)

### Modified Entities

**AppSettings** -- gains three properties:
- QaHealthThresholds (QaHealthThresholdConfig, default new())
- QualityHealthWeight (int, default 20, range 0-100)
- QualitySubScoreWeights (QualitySubScoreWeightConfig, default new())

**Ticket** -- gains one property:
- ParentTicketKey (string?, nullable) -- populated from Jira DTO `Fields.Parent?.Key` when the parent is NOT an epic (sub-task -> parent story relationship). Used for BR9 sub-task coverage inheritance.

### Modified Records

**HealthScoreResult** (in SprintSummaryService.cs) -- gains:
- QualitySubScore (decimal?, nullable -- null only when !hasQaData; non-null in observation mode when qualityWeight=0)
- QualityRag (string?, nullable -- same null semantics)
- QualityBreakdown (QualityBreakdownResult?, nullable -- same null semantics)

**QualityBreakdownResult** -- new record:
- CoverageScore (decimal)
- CoverageWeight (int)
- PassRateScore (decimal)
- PassRateWeight (int)

## Data Model Changes

### Modified Tables

| Table | Change |
|-------|--------|
| AppSettings | Add owned JSON columns: QaHealthThresholds, QualitySubScoreWeights. Add column: QualityHealthWeight (int, default 20) |
| Tickets | Add column: ParentTicketKey (string, nullable, max 64) |

### Migrations

Single migration combining both changes:

```
dotnet ef migrations add AddQaHealthSettings -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

No seed data needed -- new AppSettings fields have defaults, ParentTicketKey is populated on next sprint sync.

## Implementation Steps

### Step 1: Add QA Settings to AppSettings Entity

Extend the AppSettings domain entity with QA health threshold and weight properties.

**Follow** `domain-patterns` -- value object creation and property additions to existing entity.

**Value objects to create:**

**QaHealthThresholdConfig** -- same pattern as `HealthThresholdConfig`:
- CoverageGreen (decimal, default 80)
- CoverageAmber (decimal, default 50)
- ExecutionGreen (decimal, default 80)
- ExecutionAmber (decimal, default 50)
- PassRateGreen (decimal, default 90)
- PassRateAmber (decimal, default 70)
- GetEqualityComponents yields all six properties

**QualitySubScoreWeightConfig** -- same pattern as `HealthWeightConfig`:
- CoverageWeight (int, default 50)
- PassRateWeight (int, default 50)
- GetEqualityComponents yields both properties

**Files:**
- Create: `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/QaHealthThresholdConfig.cs`
- Create: `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/QualitySubScoreWeightConfig.cs`
- Modify: `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`
  - Add: `public QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();`
  - Add: `public int QualityHealthWeight { get; set; } = 20;`
  - Add: `public QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();`
  - Update `CreateDefault()` to include the three new fields with defaults

**Pattern reference:** `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/HealthThresholdConfig.cs`, `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/HealthWeightConfig.cs`

**Dependencies:** None

---

### Step 2: Update AppSettings EF Configuration and Migration

Add EF configuration for the new AppSettings JSON columns and generate the migration.

**Follow** `persistence-patterns`.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs`
  - Add: `builder.OwnsOne(s => s.QaHealthThresholds, b => b.ToJson());`
  - Add: `builder.OwnsOne(s => s.QualitySubScoreWeights, b => b.ToJson());`
  - QualityHealthWeight needs no special config (int maps directly)

**Pattern reference:** Existing `OwnsOne(...ToJson())` calls in the same file for `HealthThresholds` and `HealthWeights`.

**Migration note:** Do NOT run `dotnet ef migrations add` after this step. The migration is deferred to Step 4, which combines both AppSettings and Ticket schema changes into a single migration.

**Dependencies:** Step 1

---

### Step 3: Add ParentTicketKey to Ticket Entity and Sync Mapper

Add the ParentTicketKey field to the Ticket entity so sub-task coverage inheritance (BR9) can look up parent tickets at query time. Update the Jira-to-domain mapping to populate this field.

**Follow** `domain-patterns` -- property addition to existing entity, factory method update.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Domain/Ticket/Ticket.cs`
  - Add: `public string? ParentTicketKey { get; set; }`
- Modify: `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs`
  - In the `FromJira(JiraIssue dto)` factory method, add mapping:
    - `ParentTicketKey` = `dto.Fields.Parent?.Key` ONLY when the parent's issue type is NOT "Epic" (i.e., `dto.Fields.Parent?.Fields.Issuetype?.Name != "Epic"`). If the parent IS an epic, leave null -- the EpicKey field already handles that relationship. This distinguishes sub-task-to-parent-story links from story-to-epic links.

**Pattern reference:** Existing `EpicKey` mapping in the same factory method (lines 11-12 of `Behaviors/Ticket.cs`).

**Dependencies:** None

---

### Step 4: Update Ticket EF Configuration and Combined Migration

Add the ParentTicketKey column configuration and generate the combined migration.

**Follow** `persistence-patterns`.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs`
  - Add: `builder.Property(t => t.ParentTicketKey).HasMaxLength(64);`
  - No foreign key -- parent ticket may not exist in the database (it may be in a different project or not synced). This is a soft reference, same as EpicKey.

**Migration:** Combine Steps 2 and 4 into a single migration:
```
dotnet ef migrations add AddQaHealthSettingsAndParentTicketKey -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

**Dependencies:** Steps 2, 3

---

### Step 5: Extend TestExecutionRepository for Sprint-Scoped Queries

Add repository methods needed by the QA metrics computation service. These methods join through TestExecutionLink to SprintMembership to scope TEs to a specific sprint.

**No matching skill** -- complex cross-entity query methods joining multiple tables.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs`

**Methods to add:**

1. `GetTestExecutionsForSprintAsync(int sprintId, CancellationToken ct)` -- returns `List<TestExecution>` (with Links and TestRuns included) that are linked to tickets in the given sprint. A TE belongs to a sprint when any of its TestExecutionLinks point to a ticket that has a SprintMembership with that sprintId. Per BR8, when a TE links to tickets in multiple sprints, attribute it to the most recent sprint -- so include a TE only if `sprintId` is the maximum SprintId across all its linked tickets' memberships.

   Query strategy:
   - Join TestExecutionLinks -> SprintMemberships to find all TEs linked to the sprint
   - Group by TestExecution.Id
   - Filter: include only where `Max(SprintMembership.SprintId) == sprintId` (BR8 tiebreaker)
   - Exclude cancelled TEs (where `IsCancelled == true`) per BR7
   - Include Links and TestRuns via eager loading

2. `GetFeatureTicketsWithCoverageAsync(int sprintId, List<string> excludedStatuses, List<string> orderedStages, int startIndex, DateTime sprintStart, DateTime sprintEnd, CancellationToken ct)` -- returns feature tickets in the sprint's active scope along with coverage and failure data. Returns `List<TicketCoverageInfo>` where `TicketCoverageInfo` is a record with: TicketKey, Summary, AssigneeName, StoryPoints, ParentTicketKey, IssueType, HasCoverage (bool), FailedRunCount (int), TotalRunCount (int).

   **Active scope filtering criteria** (same logic as `SprintSummaryService.ComputeMetrics`):
   - `RemovedAt == null` (not removed from sprint)
   - `IssueType != "Bug"` (feature tickets only)
   - `FinalStatus` not in `excludedStatuses` (not excluded-from-scope)
   - `IsStartedInSprint == true` (transitioned past cycle time start boundary, using `TransitionAttributionChecker.IsStartedInSprint`)
   - Has effective SP (`GetEffectiveSp(defaultSpPerBug) != null`)

   **Coverage determination:** A ticket HasCoverage = true when it has at least one non-cancelled TestExecutionLink where LinkType = Tests. The query must also account for sub-task inheritance (BR9): a sub-task without its own TE links inherits coverage from its parent ticket's TE links. This requires joining Ticket.ParentTicketKey to check the parent's coverage status.

   **Failure counts:** For each ticket, compute FailedRunCount and TotalRunCount across all non-cancelled linked TEs' test runs (only runs with Status = Pass or Fail count toward TotalRunCount). Per BR5, if a ticket has multiple TEs and any TE has a FAIL run, the ticket is considered failing.

**Pattern reference:** Existing `GetByIssueIdsAsync` method in the same file. `SprintRepository.GetSprintsWithMembershipsAsync` for multi-table eager loading.

**Dependencies:** Steps 3, 4

---

### Step 6: Create QaMetricsService

Create the computation service that calculates all QA metrics for a sprint. This is the QA equivalent of `SprintSummaryService` -- pure computation, no database access (receives pre-loaded data).

**No matching skill** -- metric computation patterns are a gap. Full inline detail required.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs`
  - Public static class with methods extracted from `SprintSummaryService`: `ScoreHigherIsBetter`, `ScoreLowerIsBetter`, `MetricRag`, `CompositeRag`.
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`
  - Replace private static `ScoreHigherIsBetter`, `ScoreLowerIsBetter`, `MetricRag`, `CompositeRag` methods with calls to `HealthScoreCalculator`.
  - Add non-positional `Rag` property to `MetricCard` record: `public string? Rag { get; init; }`.
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs`

**Service shape:**

Class `QaMetricsService` with a single public method:

```
QaMetricsResult ComputeQaMetrics(
    List<TestExecution> sprintTEs,
    List<SprintMembership> activeMemberships,
    List<StatusTransition> statusTransitions,
    Sprint selectedSprint,
    Sprint? priorSprint,
    List<TestExecution>? priorSprintTEs,
    List<SprintMembership>? priorMemberships,
    List<Sprint> sparklineWindow,
    Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
    Dictionary<int, List<SprintMembership>> sparklineMembershipsBySprintId,
    AppSettings settings,
    string? subTeam)
```

**Records to define** (in the same file, above the service class -- following SprintSummaryService pattern):

- `QaMetricsResult`: hasQaData (bool), coverageRate (MetricCard), executionRate (MetricCard), passRate (MetricCard), bugsFound (int), qualitySubScore (decimal), qualityBreakdown (QualityBreakdownInfo), untestedCount (int), failingCount (int)
- `QualityBreakdownInfo`: coverageScore (decimal), coverageWeight (int), passRateScore (decimal), passRateWeight (int)

**Computation logic:**

The service computes metrics for the selected sprint and optionally for the prior sprint (for deltas) and a sparkline window (up to 4 trailing sprints).

**Active scope determination:** Reuse the same logic as `SprintSummaryService.ComputeMetrics` -- filter memberships for: not removed (`RemovedAt == null`), not bug (`IssueType != "Bug"`), not excluded status, has effective SP, and `IsStartedInSprint` returns true (transitioned past the cycle time start boundary). Count tickets, not SP. This is the Coverage Rate denominator.

**Coverage Rate (BR1):**
- Numerator: feature tickets in active scope that have at least one non-cancelled TestExecutionLink where LinkType = Tests. For sub-tasks without own TE links, check parent ticket's TE links via ParentTicketKey (BR9).
- Denominator: total feature tickets in active scope.
- Division by zero (BR11): 0 denominator -> 0%.

**Execution Rate (BR2):**
- Numerator: covered tickets (from Coverage Rate numerator) where at least one linked TE has a TestRun with Status = Pass or Status = Fail.
- Denominator: covered tickets.
- Division by zero (BR12): 0 denominator -> 0%.

**Pass Rate (BR3):**
- Numerator: count of TestRuns with Status = Pass across all non-cancelled TEs in the sprint.
- Denominator: count of TestRuns with Status = Pass or Status = Fail across all non-cancelled TEs.
- Division by zero (BR13): 0 denominator -> 0%.

**Bugs Found (BR4):**
- Count unique TicketKeys from TestExecutionLinks where LinkType = Blocks, across non-cancelled TEs, where the linked ticket's IssueType = "Bug".

**Quality Sub-Score (BR16):**
- coverageScore = ScoreHigherIsBetter(coverageRate, settings.QaHealthThresholds.CoverageGreen, settings.QaHealthThresholds.CoverageAmber)
- passRateScore = ScoreHigherIsBetter(passRate, settings.QaHealthThresholds.PassRateGreen, settings.QaHealthThresholds.PassRateAmber)
- qualitySubScore = (coverageScore * coverageWeight + passRateScore * passRateWeight) / (coverageWeight + passRateWeight)
- Reuse the existing `ScoreHigherIsBetter` method from SprintSummaryService (extract to a static helper or call directly).

**Delta (BR21):** Current sprint value minus prior sprint value. Direction: up/down/flat. All QA metrics use "positive-up" polarity. Null when no prior sprint has QA data.

**Sparkline (BR22):** Trailing 4-sprint window. Compute the metric for each sprint in the window. Only include sprints that have QA data (non-empty TE list).

**Sub-team filter (BR23):** When subTeam is specified, filter memberships to tickets assigned to developers in that sub-team. Same pattern as `SprintSummaryService.FilterMemberships`.

**hasQaData determination (per user decisions):** true when Xray is enabled (settings.XrayEnabled) AND the sprint has been synced for QA data. A synced sprint with zero TEs still has `hasQaData = true` -- this penalizes sprints with no test executions by including a 0% Quality sub-score in the composite. `hasQaData = false` only when Xray is disabled or the sprint has never been QA-synced. The "synced" state is determined by whether the sprint's tickets have been processed through XrayIssueSyncService (check: the sprint exists in the sync history, or simply: Xray is enabled and the sprint has been synced at all -- use the existing Sprint.SyncedAt combined with XrayEnabled as the proxy).

**MetricCard Rag extension:** The existing `MetricCard` positional record does not have a RAG field. Add `Rag` as a **non-positional `init` property** (not a positional parameter) to avoid breaking the 5 existing `BuildMetricCard` call sites in `SprintSummaryService` (lines 156-191). Declaration: `public string? Rag { get; init; }` after the positional parameters. Existing delivery cards will have `Rag = null` (no RAG on delivery metric cards today), preserving backward compatibility. QA metric cards set `Rag` via object initializer syntax: `card with { Rag = ragValue }` or by setting it in `BuildMetricCard`.

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` -- metric computation, delta pattern, sparkline building, health score interpolation functions.

**HealthScoreCalculator extraction (happens in THIS step):** The `ScoreHigherIsBetter` and `ScoreLowerIsBetter` methods are currently private static in `SprintSummaryService`. Extract them into a new public static class `HealthScoreCalculator` so both `SprintSummaryService` and `QaMetricsService` can reference them. Also extract the `MetricRag` and `CompositeRag` helpers. After extraction, update `SprintSummaryService` to delegate to `HealthScoreCalculator` -- the private methods are replaced with calls to the shared class. Step 10 then uses this already-extracted class when modifying the composite formula; it does NOT re-extract.

**Register:** Add `QaMetricsService` as scoped in `DependencyInjection.cs`.

**Dependencies:** Steps 1, 5

---

### Step 7: Create GetQaMetrics Endpoint

Create the GET /api/sprints/{sprintId}/qa-metrics endpoint that returns all QA metrics for a sprint.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs`
  - Route: `GET /api/sprints/{sprintId}/qa-metrics`
  - Tags: "Analytics"
  - Request: `GetQaMetricsRequest` with SprintId (int, from route), SubTeam (string?, from query)
  - Handler logic:
    1. Load sprint via SprintRepository. If not found, return 404.
    2. Load AppSettings. If XrayEnabled = false, return 200 with hasQaData = false and zeroed metrics.
    3. Load sprint with memberships.
    4. Load TEs for the sprint via `TestExecutionRepository.GetTestExecutionsForSprintAsync(sprintId)`.
    5. Load status transitions for sprint tickets.
    6. Determine prior sprint and sparkline window (same pattern as GetSprintSummaryEndpoint).
    7. For prior sprint and sparkline sprints: load their TEs and memberships in bulk.
    8. Call `QaMetricsService.ComputeQaMetrics(...)`.
    9. Map result to `QaMetricsResponse` and return 200.
  - Response: `QaMetricsResponse` matching the spec's response shape.
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsQuery.cs`
  - Contains: `GetQaMetricsRequest`, `GetQaMetricsResponse`, `QaMetricsResponseValidator`

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`

**Dependencies:** Steps 5, 6

---

### Step 8: Create GetUntestedTickets Endpoint

Create the GET /api/sprints/{sprintId}/qa-metrics/untested endpoint.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetUntestedTickets/GetUntestedTicketsEndpoint.cs`
  - Route: `GET /api/sprints/{sprintId}/qa-metrics/untested`
  - Tags: "Analytics"
  - Request: `GetUntestedTicketsRequest` with SprintId (int, from route), SubTeam (string?, from query)
  - Handler logic:
    1. Load sprint. If not found, return 404.
    2. Load AppSettings. If XrayEnabled = false, return 200 with empty list.
    3. Use `TestExecutionRepository.GetFeatureTicketsWithCoverageAsync(sprintId)` to get ticket coverage data.
    4. Filter to tickets where HasCoverage = false.
    5. Apply sub-team filter if specified.
    6. Sort by StoryPoints descending (BR25).
    7. Map to `UntestedTicketsResponse`.
  - Response: `UntestedTicketsResponse` with list of `{ ticketKey, summary, assigneeName, storyPoints }`.
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetUntestedTickets/GetUntestedTicketsQuery.cs`

**Dependencies:** Step 5

---

### Step 9: Create GetFailingTickets Endpoint

Create the GET /api/sprints/{sprintId}/qa-metrics/failing endpoint.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetFailingTickets/GetFailingTicketsEndpoint.cs`
  - Route: `GET /api/sprints/{sprintId}/qa-metrics/failing`
  - Tags: "Analytics"
  - Request: `GetFailingTicketsRequest` with SprintId (int, from route), SubTeam (string?, from query)
  - Handler logic:
    1. Load sprint. If not found, return 404.
    2. Load AppSettings. If XrayEnabled = false, return 200 with empty list.
    3. Use `TestExecutionRepository.GetFeatureTicketsWithCoverageAsync(sprintId)` to get ticket data with run counts.
    4. Filter to tickets where FailedRunCount > 0.
    5. Apply sub-team filter if specified.
    6. Sort by FailedRunCount descending (BR26).
    7. Map to `FailingTicketsResponse`.
  - Response: `FailingTicketsResponse` with list of `{ ticketKey, summary, assigneeName, storyPoints, failedRunCount, totalRunCount }`.
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetFailingTickets/GetFailingTicketsQuery.cs`

**Dependencies:** Step 5

---

### Step 10: Extend Health Score with Quality Sub-Score

Modify the existing health score computation to include Quality as a fourth sub-score with the additive weight model.

**No matching skill** -- extending existing computation service.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`
  - **Extend `HealthScoreResult` record:** Add nullable fields: `QualitySubScore` (decimal?), `QualityRag` (string?), `QualityBreakdown` (QualityBreakdownResult? -- new record with CoverageScore, CoverageWeight, PassRateScore, PassRateWeight).
  - **Modify `ComputeHealthScore` method signature:** Add parameters: `decimal? qualitySubScore`, `QualityBreakdownResult? qualityBreakdown`, `int qualityWeight`, `bool hasQaData`.
  - **Modify composite formula (BR18) -- three-way branching per user decisions:**
    - When `hasQaData && qualityWeight > 0`: totalWeight = deliverySum + qualityWeight. Numerator includes `qualitySubScore * qualityWeight`. QualitySubScore, QualityRag, QualityBreakdown are all populated (non-null).
    - When `hasQaData && qualityWeight == 0` (observation mode): totalWeight = deliverySum only (quality excluded from composite). But QualitySubScore, QualityRag, and QualityBreakdown are still computed and returned (non-null). The health tooltip shows "Quality: {score}% (weight: 0)".
    - When `!hasQaData` (Xray disabled or sprint not synced): totalWeight = deliverySum only. QualitySubScore = null, QualityRag = null, QualityBreakdown = null.
  - **Modify RAG bands:** Unchanged (green >= 75, amber >= 40, red < 40) per BR18.
  - **`ScoreHigherIsBetter`/`ScoreLowerIsBetter` already extracted** in Step 6 -- this step just uses `HealthScoreCalculator`. No re-extraction needed.

- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
  - After computing delivery metrics, check if Xray is enabled and sprint has QA data.
  - If yes: load TEs for the sprint, compute qualitySubScore and qualityBreakdown using `QaMetricsService` (or inline the sub-score formula using `HealthScoreCalculator`).
  - Pass quality data to `ComputeHealthScore`.
  - This keeps the SprintSummaryResponse as the single entry point for health score data.

**Performance note:** The sprint summary endpoint now needs to load TEs when Xray is enabled. This is a single additional query per request. The TEs are loaded only for the selected sprint (not the sparkline window) because the health score only needs the current sprint's quality sub-score.

**Pattern reference:** Existing `ComputeHealthScore` method at line 519 of `SprintSummaryService.cs`.

**Dependencies:** Steps 1, 5, 6

---

### Step 11: Extend SaveHealthConfig Endpoint for QA Fields

Extend the existing PUT /api/settings/health-config endpoint to accept and validate QA health configuration fields.

**Follow** `create-feature` (modification to existing endpoint).

**Nullability model: non-nullable (full replace).** The QA fields use the same non-nullable model as existing delivery fields -- every save sends all fields (delivery + QA). This matches the existing `SaveHealthConfigRequest` pattern where `HealthThresholds` and `HealthWeights` are non-nullable and always sent. The frontend always sends the full health config payload.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigEndpoint.cs`
  - Extend `SaveHealthConfigRequest`:
    - Add: `QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();` (non-nullable, default)
    - Add: `int QualityHealthWeight { get; set; } = 20;` (non-nullable, default)
    - Add: `QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();` (non-nullable, default)
  - Extend `SaveHealthConfigRequestValidator`:
    - QualityHealthWeight: InclusiveBetween(0, 100).
    - QaHealthThresholds: each green threshold > corresponding amber threshold (all QA metrics are higher-is-better, so green > amber). Each value InclusiveBetween(0, 100).
    - QualitySubScoreWeights: CoverageWeight >= 1, PassRateWeight >= 1 (BR14). Both minimum 1 inherently prevents both-zero.
    - **Existing delivery weight validation unchanged** (sum to 100 rule stays).
  - Extend handler: assign all three new fields from request to existing AppSettings unconditionally (same pattern as existing `existing.HealthThresholds = req.HealthThresholds`).

**Dependencies:** Steps 1, 2

---

### Step 12: Extend GetSettings Endpoint for QA Fields

Add QA settings fields to the GET /api/settings response so the frontend can display current configuration.

**Follow** `create-feature` (modification to existing endpoint).

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs`
  - Extend `GetSettingsResponse`:
    - Add: `QaHealthThresholdConfig QaHealthThresholds { get; set; }`
    - Add: `int QualityHealthWeight { get; set; }`
    - Add: `QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; }`
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`
  - Map the three new fields from settings entity to response.

**Dependencies:** Steps 1, 2

---

### Step 13: Frontend -- Dashboard QA Section and Health Score Extension

Add the Test Quality section to the Dashboard view, extend the health score badge to show the Quality sub-score, and wire tooltips. Add the QA API module and extend the dashboard store.

**Follow** `vue-patterns` for component patterns. **Follow** `pinia-patterns` for store extensions.

**Files:**

**Types:**
- Modify: `client/src/types/index.ts`
  - Add `QaMetricsResponse` interface: hasQaData (boolean), coverageRate (MetricCard), executionRate (MetricCard), passRate (MetricCard), bugsFound (number), qualitySubScore (number), qualityBreakdown ({ coverageScore: number, coverageWeight: number, passRateScore: number, passRateWeight: number }), untestedCount (number), failingCount (number)
  - Add `UntestedTicket` interface: ticketKey (string), summary (string), assigneeName (string | null), storyPoints (number | null)
  - Add `FailingTicket` interface: ticketKey (string), summary (string), assigneeName (string | null), storyPoints (number | null), failedRunCount (number), totalRunCount (number)
  - Add `UntestedTicketsResponse` interface: tickets (UntestedTicket[])
  - Add `FailingTicketsResponse` interface: tickets (FailingTicket[])
  - Extend `MetricCard` interface: add `rag: string | null` (nullable -- null for existing delivery cards, populated for QA cards)
  - Extend `HealthScoreResult` interface: add qualitySubScore (number | null), qualityRag (string | null), qualityBreakdown ({ coverageScore: number, coverageWeight: number, passRateScore: number, passRateWeight: number } | null)
  - Add `QaHealthThresholdConfig` interface: coverageGreen (number), coverageAmber (number), executionGreen (number), executionAmber (number), passRateGreen (number), passRateAmber (number)
  - Add `QualitySubScoreWeightConfig` interface: coverageWeight (number), passRateWeight (number)
  - Extend `AppSettings` interface: add qaHealthThresholds (QaHealthThresholdConfig), qualityHealthWeight (number), qualitySubScoreWeights (QualitySubScoreWeightConfig)

**API module:**
- Modify: `client/src/api/analytics.ts`
  - Add: `getQaMetrics(sprintId: number, subTeam?: string): Promise<QaMetricsResponse>` -- GET /api/sprints/{sprintId}/qa-metrics
  - Add: `getUntestedTickets(sprintId: number, subTeam?: string): Promise<UntestedTicketsResponse>` -- GET /api/sprints/{sprintId}/qa-metrics/untested
  - Add: `getFailingTickets(sprintId: number, subTeam?: string): Promise<FailingTicketsResponse>` -- GET /api/sprints/{sprintId}/qa-metrics/failing

**Store:**
- Modify: `client/src/stores/dashboardStore.ts`
  - Add state: `qaMetrics` (ref<QaMetricsResponse | null>), `untestedTickets` (ref<UntestedTicket[]>), `failingTickets` (ref<FailingTicket[]>), `qaLoading` (ref<boolean>)
  - Add action: `fetchQaMetrics()` -- calls getQaMetrics with selectedSprintId and selectedSubTeam. Called after `fetchSummary()` completes (not in parallel -- needs sprint context).
  - Add actions: `fetchUntestedTickets()`, `fetchFailingTickets()` -- called on-demand when user expands the respective sections (lazy load).
  - Modify `fetchSummary()`: after fetching summary, also call `fetchQaMetrics()`.
  - Modify `selectSprint()` and `selectSubTeam()`: clear qaMetrics, untestedTickets, failingTickets when sprint/subteam changes.

**Components:**
- Create: `client/src/components/dashboard/QaMetricsSection.vue`
  - Props: `qaMetrics` (QaMetricsResponse), `xrayEnabled` (boolean)
  - Renders:
    - Section header: "Test Quality" with appropriate styling
    - Three MetricCard components for Coverage Rate, Execution Rate, Pass Rate (reuse existing MetricCard component). Pass RAG color as prop if MetricCard supports it, otherwise apply RAG styling via a wrapper.
    - Each card: info tooltip from `help.tooltips.md` content (hardcoded strings, same pattern as existing MetricCard tooltips).
    - "Bugs found: N" annotation below cards (hidden when 0). Tooltip from help.tooltips.md.
    - "Untested tickets (N)" expandable section. On expand, calls `fetchUntestedTickets()`. Shows ticket key, summary, assignee, SP. Tooltip from help.tooltips.md.
    - "Failing tickets (N)" expandable section. On expand, calls `fetchFailingTickets()`. Shows ticket key, summary, assignee, SP, failed/total runs. Tooltip from help.tooltips.md.
  - Empty states (Flow 4):
    - Xray disabled: do not render this component at all (parent view checks).
    - Xray enabled, hasQaData = false: show "Sync sprint to load QA data" prompt.
    - Xray enabled, hasQaData = true, zero values: cards show 0%, lists empty.

- Modify: `client/src/components/dashboard/MetricCard.vue`
  - Add RAG indicator rendering when `metric.rag` is non-null. Display a colored dot or border matching the RAG value (green/amber/red) using the existing `ragBgClass`/`ragClass` helper pattern from HealthScoreBadge.vue. When `metric.rag` is null (delivery cards), no RAG indicator is shown -- preserving existing behavior.

- Modify: `client/src/components/dashboard/HealthScoreBadge.vue`
  - Add Quality sub-score display alongside Completion, Disruption, Carry-Over.
  - Show only when `healthScore.qualitySubScore !== null`.
  - Quality column: score value with RAG color, label "Quality".
  - Update tooltip text to include Quality when present.
  - Show quality breakdown on hover or in a nested tooltip: "Coverage: {score} x {weight}, Pass Rate: {score} x {weight}".

- Modify: `client/src/views/DashboardView.vue`
  - Import `QaMetricsSection` component.
  - Import `useSettingsStore` from `'../stores/settingsStore'` and instantiate: `const settingsStore = useSettingsStore()`.
  - In `onMounted`: add `await settingsStore.fetchSettings()` (can run in parallel with `store.initialize()` via `Promise.all`). This ensures `settingsStore.settings.xrayEnabled` is available before rendering.
  - Add the QA section below the delivery metrics section (after MetricCard grid, before Top Epics).
  - Gate rendering: `v-if="settingsStore.settings.xrayEnabled"` on `QaMetricsSection`.

**Help tooltips:** Wire the tooltip text from `docs/specs/F26-SprintTestCoverage/definition/help.tooltips.md` to each corresponding UI element using the `InfoTooltip` component (same pattern as existing MetricCard tooltips).

**Dependencies:** Steps 7, 8, 9, 10, 12

---

### Step 14: Frontend -- Settings UI Quality Sub-Group

Add the Quality configuration sub-group to the Settings Health section, visible only when Xray is enabled.

**Follow** `vue-patterns` for component patterns. **Follow** `pinia-patterns` for store extensions.

**Files:**

**Full saveHealthConfig modification chain (all three files must be updated):**

**1. API module:**
- Modify: `client/src/api/settings.ts`
  - Extend `saveHealthConfig` function signature to accept the QA health fields alongside existing thresholds/weights:
    ```
    saveHealthConfig(
      healthThresholds: HealthThresholdConfig,
      healthWeights: HealthWeightConfig,
      qaHealthThresholds: QaHealthThresholdConfig,
      qualityHealthWeight: number,
      qualitySubScoreWeights: QualitySubScoreWeightConfig
    )
    ```
  - Include all five fields in the JSON body sent to PUT /api/settings/health-config.

**2. Store:**
- Modify: `client/src/stores/settingsStore.ts`
  - Extend initial `settings` ref with: `qaHealthThresholds` (default values), `qualityHealthWeight` (20), `qualitySubScoreWeights` (default values)
  - Extend `saveHealthConfigAction` signature to accept the three new QA parameters and pass them through to the API call.

**3. View (call site):**
- Modify: `client/src/views/SettingsView.vue`
  - Update the `saveHealthConfigPanel()` call site (approximately line 104) to pass the three new QA fields from the form state to `settingsStore.saveHealthConfigAction(...)`.
  - Add form state fields: `form.qaHealthThresholds`, `form.qualityHealthWeight`, `form.qualitySubScoreWeights` with defaults synced from the store.
  - In the Health section (the tab/panel that shows delivery thresholds and weights):
    - Add a "Quality" sub-group that appears ONLY when `settings.xrayEnabled` is true.
    - Quality sub-group contains:
      - Coverage Rate thresholds: green and amber inputs (defaults 80/50). Tooltip from help.tooltips.md.
      - Execution Rate thresholds: green and amber inputs (defaults 80/50). Tooltip from help.tooltips.md.
      - Pass Rate thresholds: green and amber inputs (defaults 90/70). Tooltip from help.tooltips.md.
      - Quality Health Weight: single input (default 20, range 0-100). Tooltip from help.tooltips.md.
      - Coverage/Pass Rate weights: two inputs (defaults 50/50, minimum 1). Tooltip from help.tooltips.md.
    - Include all thresholds and weights in the existing Health save action. The save button already exists -- extend its payload.
    - Client-side validation: Quality Health Weight 0-100, each sub-score weight >= 1, each threshold 0-100, green > amber for each metric.

**Dependencies:** Steps 11, 12, 13

---

## Cross-Service Changes

None. Single-service application.

## Migration Notes

Run the combined migration after Steps 2 and 4:

```
dotnet ef migrations add AddQaHealthSettingsAndParentTicketKey -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

No seed data needed. New AppSettings fields have defaults via entity constructor. ParentTicketKey is populated on next sprint sync.

The migration is additive-only (new columns, no drops or renames). Existing data is unaffected.

**Note:** After this migration, a sprint re-sync is required to populate ParentTicketKey on existing tickets. Until re-synced, sub-task coverage inheritance (BR9) will not work for previously synced tickets -- they will be treated as having no parent. This is acceptable behavior (graceful degradation).

## Testing Strategy

### QA Metrics Computation
- Sprint with mixed coverage: some tickets with TEs, some without -> correct coverage rate
- Sprint with mixed execution: some TEs with runs, some without -> correct execution rate
- Sprint with mixed pass/fail runs -> correct pass rate
- Sprint with cancelled TEs -> excluded from all metrics
- Sprint with sub-tasks inheriting parent coverage -> correct coverage attribution
- Sprint with TE linked to tickets in multiple sprints -> TE attributed to most recent sprint only
- Sprint with zero feature tickets -> all rates 0%
- Sprint with zero covered tickets -> execution rate 0%
- Sprint with zero executed runs -> pass rate 0%

### Health Score Integration
- Quality sub-score computed correctly from coverage and pass rate scores with configured weights
- Composite score includes Quality with additive weight when Xray enabled and QA data exists and qualityWeight > 0
- Composite score excludes Quality from composite when Xray disabled (QualitySubScore = null in response)
- Composite score excludes Quality from composite when sprint not synced (QualitySubScore = null in response)
- Quality Health Weight = 0 (observation mode) -> Quality sub-score IS computed and returned (non-null), but excluded from composite numerator and totalWeight. Health tooltip shows "Quality: {score}% (weight: 0)"
- Xray enabled, synced sprint with zero TEs -> hasQaData = true, Quality sub-score = 0%, IS included in composite (penalizes untested sprints)
- Delivery weight sum-to-100 validation unchanged after Quality addition

### Ticket Lists
- Untested tickets sorted by SP descending
- Failing tickets sorted by failed run count descending
- Sub-team filter applied to both lists
- Empty lists when all tickets covered / all passing

### Settings
- Save QA health thresholds -> persisted and returned in GET
- Save quality weight 0 -> accepted (observation mode)
- Save sub-score weights both >= 1 -> accepted
- Save sub-score weight < 1 -> rejected by validator
- Quality settings section hidden when Xray disabled

### Empty States
- Xray disabled -> QA section hidden, health score delivery-only (QualitySubScore = null)
- Xray enabled, sprint not synced -> "Sync sprint to load QA data" prompt, hasQaData = false, QualitySubScore = null
- Xray enabled, synced, zero TEs -> hasQaData = true, cards show 0%, Quality sub-score = 0%, included in composite (penalizes untested sprints)

### Sub-team Filter
- All three QA endpoints accept ?subTeam and scope results correctly
- Delta and sparkline scoped to same sub-team

## Open Questions

None.
