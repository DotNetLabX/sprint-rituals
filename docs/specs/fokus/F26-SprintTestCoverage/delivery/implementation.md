# Sprint Test Coverage — Implementation (Phase 1: Steps 1-12, Phase 2: Steps 13-14)

## Files Created

- `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/QaHealthThresholdConfig.cs` — New value object with 6 threshold properties (CoverageGreen/Amber, ExecutionGreen/Amber, PassRateGreen/Amber), follows HealthThresholdConfig pattern
- `src/Services/Fokus/Fokus.Domain/Settings/ValueObjects/QualitySubScoreWeightConfig.cs` — New value object with CoverageWeight and PassRateWeight, follows HealthWeightConfig pattern
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260515063240_AddQaHealthSettingsAndParentTicketKey.cs` — Combined migration adding QaHealthThresholds and QualitySubScoreWeights (JSON columns) + QualityHealthWeight (int) to AppSettings, and ParentTicketKey (nullable string, max 64) to Tickets
- `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs` — New public static class with ScoreHigherIsBetter, ScoreLowerIsBetter, CompositeRag, MetricRag extracted from SprintSummaryService
- `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs` — New scoped service computing all QA metrics; includes QaMetricsResult and QualityBreakdownInfo records. Implements BR1-BR4, BR16, BR21-BR23
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` — GET /api/sprints/{sprintId}/qa-metrics endpoint
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsQuery.cs` — Request, response, and validator types for GetQaMetrics
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetUntestedTickets/GetUntestedTicketsEndpoint.cs` — GET /api/sprints/{sprintId}/qa-metrics/untested endpoint
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetUntestedTickets/GetUntestedTicketsQuery.cs` — Request, response, and validator types for GetUntestedTickets
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetFailingTickets/GetFailingTicketsEndpoint.cs` — GET /api/sprints/{sprintId}/qa-metrics/failing endpoint
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetFailingTickets/GetFailingTicketsQuery.cs` — Request, response, and validator types for GetFailingTickets

## Files Modified

- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added QaHealthThresholds, QualityHealthWeight, QualitySubScoreWeights properties; updated CreateDefault() with defaults
- `src/Services/Fokus/Fokus.Domain/Ticket/Ticket.cs` — Added ParentTicketKey (string?) property
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` — Added ParentTicketKey mapping in FromJira(): populated when parent is not an Epic (null when parent is Epic or absent)
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — Added OwnsOne(...ToJson()) for QaHealthThresholds and QualitySubScoreWeights
- `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs` — Added HasMaxLength(64) for ParentTicketKey
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — Added TicketCoverageInfo record; added GetTestExecutionsForSprintAsync (BR7+BR8) and GetFeatureTicketsWithCoverageAsync (active scope filter, coverage/failure counts, BR9 sub-task inheritance); private helpers GetEffectiveSp and GetStageIndex to avoid Persistence depending on API layer
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Added Rag non-positional init property to MetricCard; extracted ScoreHigherIsBetter/ScoreLowerIsBetter/CompositeRag/MetricRag to delegate to HealthScoreCalculator; added QualityBreakdownResult record and extended HealthScoreResult with nullable Quality fields; extended ComputeHealthScore with quality parameters and three-way branching (BR18); extended ComputeSummary signature with quality parameters
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Added TestExecutionRepository and QaMetricsService injection; added step 11 to load TEs and compute quality sub-score when XrayEnabled; updated ComputeSummary call to pass quality data
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigEndpoint.cs` — Extended SaveHealthConfigRequest with 3 QA fields; extended validator with QA threshold (green > amber) and sub-score weight (>= 1) rules; extended handler to assign QA fields
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Extended GetSettingsResponse with QaHealthThresholds, QualityHealthWeight, QualitySubScoreWeights
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Extended mapping to populate the 3 new QA settings fields in response
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered QaMetricsService as scoped

## Key Decisions

- **TransitionAttributionChecker not referenced from Persistence**: The static helper is in the API layer. `GetFeatureTicketsWithCoverageAsync` in Persistence replicates the IsStartedInSprint logic inline (simple LINQ predicate on transitions) rather than calling the API-layer helper. Private `GetStageIndex` and `GetEffectiveSp` helpers added to avoid circular dependencies.
- **QaMetricsService sparkline receives pre-loaded dictionaries**: The endpoint is responsible for loading TE lists per sprint in the window and passing them in — same separation of concerns as SprintSummaryService.
- **GetSprintSummaryEndpoint Quality sub-score**: Passes empty sparkline window to QaMetricsService (delta and sparkline not needed for health score — only QualitySubScore and QualityBreakdown are used).
- **hasQaData determination**: Set to `true` whenever `XrayEnabled = true`. A synced sprint with zero TEs still contributes a 0% quality sub-score to the composite (penalizes untested sprints per spec).
- **Sub-team filter in GetUntestedTickets/GetFailingTickets**: Applied post-load using the sprint's membership data (ticket assignee sub-team lookup). The repository doesn't have sub-team context so filtering happens in the endpoint.

## Deviations from Plan

- None. All steps implemented as specified.

## Review Cycle 1 Fixes

- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — Added `.ThenInclude(l => l.Ticket)` after `.Include(te => te.Links)` in `GetTestExecutionsForSprintAsync` so that `link.Ticket` is eagerly loaded, fixing `ComputeBugsFound` always returning 0 (HIGH).
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260515063240_AddQaHealthSettingsAndParentTicketKey.cs` — Changed `defaultValue: 0` to `defaultValue: 20` for `QualityHealthWeight` to match entity default and `CreateDefault()`, so existing rows get observation-ready default rather than silent 0 (MEDIUM).
- `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs` — Removed dead `ContainsKey` guard in `BuildQaSparkline` (caller always populates all window sprint keys); removed redundant `!te.IsCancelled` in-memory filters in `ComputeQaMetrics`, prior sprint delta block, and sparkline block — repository already excludes cancelled TEs at the DB level; added comments documenting pre-filtered input (MEDIUM + LOW).
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — Added comment on `"Cancelled"` string documenting coupling to `IsCancelled` computed property and why the raw string is required for EF SQL translation (LOW).

---

## Phase 2: Steps 13-14 (Frontend)

### Files Created

- `client/src/components/dashboard/QaMetricsSection.vue` — New component: Test Quality section card with 3 MetricCard instances (Coverage Rate, Execution Rate, Pass Rate), bugs found count with delta annotation, and two expandable lists (Untested Tickets, Failing Tickets). Emits `loadUntested` / `loadFailing` events on first expand. Tooltips wired from help.tooltips.md.

### Files Modified

- `client/src/types/index.ts` — Added `rag: string | null` to `MetricCard`; added nullable `qualitySubScore`, `qualityRag`, `qualityBreakdown` to `HealthScoreResult`; added `QualityBreakdownResult` interface; added `QaHealthThresholdConfig` and `QualitySubScoreWeightConfig` interfaces; added 3 QA fields to `AppSettings`; added `QaMetricsResponse`, `QualityBreakdownResponse`, `UntestedTicket`, `FailingTicket`, `UntestedTicketsResponse`, `FailingTicketsResponse` interfaces.
- `client/src/api/analytics.ts` — Added `getQaMetrics`, `getUntestedTickets`, `getFailingTickets` functions mapping to backend sprint-scoped endpoints.
- `client/src/api/settings.ts` — Extended `saveHealthConfig` with 3 QA params (`qaHealthThresholds`, `qualityHealthWeight`, `qualitySubScoreWeights`); added `QaHealthThresholdConfig` and `QualitySubScoreWeightConfig` to imports.
- `client/src/stores/dashboardStore.ts` — Added `qaMetrics`, `untestedTickets`, `failingTickets`, `qaLoading` state; added `fetchQaMetrics`, `fetchUntestedTickets`, `fetchFailingTickets` actions; `clearQaState` called on sprint/subteam change; QA errors are non-fatal (section stays hidden, no store-level error set).
- `client/src/stores/settingsStore.ts` — Extended imports with `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig`; added QA defaults to `settings` ref initializer; extended `saveHealthConfigAction` with 3 QA params and updated optimistic state merge.
- `client/src/components/dashboard/MetricCard.vue` — Added RAG-based border and value coloring when `metric.rag` is non-null; added tooltip entries for Coverage Rate, Execution Rate, Pass Rate metric names.
- `client/src/components/dashboard/HealthScoreBadge.vue` — Added Quality sub-score column (conditionally rendered when `healthScore.qualitySubScore !== null`) next to the delivery sub-scores.
- `client/src/views/DashboardView.vue` — Imported `QaMetricsSection` and `useSettingsStore`; `onMounted` fetches settings in parallel with `initialize()` then calls `fetchQaMetrics` when xrayEnabled; sprint/subteam change handlers also trigger `fetchQaMetrics`; QaMetricsSection rendered below delivery metrics grid when `xrayEnabled && qaMetrics !== null`.
- `client/src/views/SettingsView.vue` — Added `InfoTooltip` import; added QA fields to `form` reactive object and `syncFromStore`; updated `saveHealthConfigPanel` call site with 5 args; added `qualityWeightsSum` helper; added Quality Settings section (v-if xrayEnabled) with quality weight, 3 threshold groups, and coverage/pass rate sub-score weights; moved Save button to its own section so it covers both delivery and QA config; save button disabled when xrayEnabled and qualityWeightsSum !== 100.

### Key Decisions

- **MetricCard RAG coloring uses border + value color**: Border changes to status color when rag is set; value text also takes the status color. Non-rag cards retain `border-border-default` and `text-text-primary` — no regression for existing delivery metrics.
- **QA errors non-fatal in dashboard store**: `fetchQaMetrics/Untested/Failing` catch errors silently. The QA section is gated on `qaMetrics !== null`, so a failed QA fetch simply hides the section rather than breaking the dashboard.
- **Settings fetch parallel in DashboardView**: `Promise.all([store.initialize(), settingsStore.fetchSettings()])` avoids sequential delay. Settings are needed to know whether to call `fetchQaMetrics` immediately after.
- **InfoTooltip wired via metricTooltip lookup**: Coverage Rate, Execution Rate, Pass Rate names added to the lookup function in MetricCard.vue rather than introducing a tooltip prop, matching the existing pattern for delivery metrics.
- **Quality Settings section v-if xrayEnabled**: Matches spec — hidden when Xray is off. The Save button section is unconditional so delivery-only users can still save health config.

### Deviations from Plan

- **`fetchQaMetrics` called from DashboardView, not inside `fetchSummary`**: The plan specified calling `fetchQaMetrics` inside `fetchSummary` in the store. Instead it is called from `DashboardView.onMounted` and the sprint/subteam change handlers directly. Reason: `fetchSummary` runs unconditionally (it has no access to `xrayEnabled` from settingsStore), while the dashboard view already has settingsStore in scope to gate the call correctly. Wiring it inside the store would require the store to depend on settingsStore, creating an undesirable cross-store dependency.
- **QaMetricsSection receives data props, not an `xrayEnabled` flag**: The plan described passing an `xrayEnabled` prop to the component. Instead the component receives `qaMetrics`, `untestedTickets`, `failingTickets`, and `qaLoading` directly, and is rendered conditionally with `v-if="settingsStore.settings.xrayEnabled && store.qaMetrics !== null"` in DashboardView. The component itself never needs to know about `xrayEnabled` — the parent gates its rendering entirely, which is cleaner separation of concerns.

---

## Phase 2 Step 1 Review Fixes

- `client/src/types/index.ts` — `FailingTicket`: renamed `failCount`→`failedRunCount`, `totalCount`→`totalRunCount` to match backend serialization; added missing `storyPoints: number | null` and `assigneeName: string | null` (HIGH).
- `client/src/types/index.ts` — `QaMetricsResponse`: added `hasQaData: boolean` as first field (backend contract); removed ghost `bugsFoundDelta` field not sent by backend (LOW × 2).
- `client/src/components/dashboard/QaMetricsSection.vue` — Removed `bugsAnnotation` computed and annotation span (referenced removed `bugsFoundDelta`); removed unused `computed` import; updated failing ticket template to use `ticket.failedRunCount`/`ticket.totalRunCount` (HIGH + LOW).
- `client/src/components/dashboard/HealthScoreBadge.vue` — Updated shared sub-score `InfoTooltip` to dynamically mention "and quality" when `qualitySubScore !== null`; added per-Quality-column `InfoTooltip` showing breakdown details from `qualityBreakdown` (MEDIUM).
