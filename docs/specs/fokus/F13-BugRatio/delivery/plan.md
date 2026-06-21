# Bug Ratio (F13)

**Feature Spec:** `docs/features/BugRatio/spec.md`

## Context

Scrum Masters need visibility into how much developer capacity goes to bug fixing vs. planned feature work. The Developers page currently shows only throughput data (F9). This feature adds a Bug Ratio tab alongside Throughput, showing per-developer and team-level bug ratio metrics with trend charts, alert badges, and issue type breakdowns. It also extends AppSettings with two new alert threshold properties.

**Service impacted:** Fokus (single service — API, Domain, Persistence, frontend)

## Scope

**In scope:**
- Two new AppSettings properties: `BugRatioAlertThreshold` (int, default 50) and `BugRatioConsecutiveSprintCount` (int, default 2)
- EF migration for the new columns
- Settings endpoint changes (GET/PUT) to include the new fields
- `BugRatioService` computation service (multi-sprint, single-sprint, alert evaluation)
- `GET /api/analytics/bug-ratio` endpoint with query/validator
- Frontend types, API module, store updates
- DevelopersView tab structure (Throughput + Bug Ratio)
- Bug Ratio tab components: metric cards, trend chart, stacked bar chart, developer table, issue type breakdown
- Frontend settings form extension for alert thresholds

**Out of scope:** Configurable bug classification rules, bug origin tracking, severity breakdown, defect leakage, per-developer drill-down page, rolling average, export, real-time updates, retrofitting F9 excluded-from-scope statuses.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | Two new scalar properties on AppSettings, EF migration | |
| 2 | extract-feature-service | Follow | BugRatioService in Features/Analytics/, response records in same file | |
| 3 | create-feature | Follow | FastEndpoints GET endpoint, query+validator, DI registration | |
| 4 | create-vue-feature | Follow | Types in index.ts matching backend response records | |
| 5 | create-vue-feature | Follow | API function in analytics.ts, identical to getScopeChange pattern | |
| 6 | pinia-patterns | Follow | Extend developersStore with bug ratio state and tab management | |
| 7 | vue-component-architecture | Follow | Tab restructure of DevelopersView, L0 view + L1 container | |
| 8 | vue-patterns | Follow | L3 props-only components: metric cards, charts, table, breakdown | |
| 9 | (none) | — | Settings form extension — straightforward field additions | Log: no "settings-form" skill |
| 10 | (none) | — | Build verification and manual testing | |

## Domain Model Changes

**Modified: `AppSettings`** (`src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`)
- Add `BugRatioAlertThreshold` (int, default 50) — percentage 0-100
- Add `BugRatioConsecutiveSprintCount` (int, default 2) — count 1-10
- Update `CreateDefault()` to include new properties

No new aggregates, entities, value objects, or domain events.

## Data Model Changes

**Modified table: `AppSettings`**
- New column: `BugRatioAlertThreshold` (INTEGER, NOT NULL, default 50)
- New column: `BugRatioConsecutiveSprintCount` (INTEGER, NOT NULL, default 2)

No new tables, relationships, or indexes.

## Implementation Steps

### Step 1: Add bug ratio alert settings to AppSettings

**What:** Add two new properties to the AppSettings entity, update the EF configuration, create a migration, and update the settings endpoints to expose the new fields.

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — add `BugRatioAlertThreshold` and `BugRatioConsecutiveSprintCount` properties with defaults
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — no changes needed (scalar int properties with defaults need no custom configuration)
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — add the two fields to `GetSettingsResponse`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — add the two fields to `SaveSettingsCommand`, add validation rules (BugRatioAlertThreshold: 0-100, BugRatioConsecutiveSprintCount: 1-10)
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — map the new fields in the `HandleAsync` AppSettings construction
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` — add the two new fields to `SaveAsync`'s `existing` update block

**Migration command:**
```
dotnet ef migrations add AddBugRatioAlertSettings -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Follow:** persistence-patterns (EF migration for new columns on existing entity)

**Dependencies:** None

### Step 2: Create BugRatioService with response records

**What:** Create the computation service that takes loaded sprints, developers, and settings, and produces multi-sprint or single-sprint bug ratio response data. This includes alert evaluation logic.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — service class + all response record types

**Response records (defined in same file, above the service class):**

```
BugRatioSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)

BugRatioTeamTrendEntry(int SprintId, decimal BugRatioPercent, decimal BugSp, decimal NonBugSp, decimal CompletedSp)

BugRatioTeamMetrics(decimal BugRatioPercent, decimal TotalBugSp, decimal TotalNonBugSp, decimal TotalCompletedSp, List<BugRatioTeamTrendEntry> PerSprintTrend)

BugRatioIssueTypeEntry(string IssueType, int TicketCount, decimal SpTotal)

BugRatioAlertStatus(bool IsActive, int ConsecutiveSprintCount)

BugRatioDeveloperSprintBreakdown(int SprintId, decimal BugSp, decimal NonBugSp, decimal CompletedSp, decimal BugRatioPercent, int BugTicketCount, int NonBugTicketCount)

BugRatioDeveloperEntry — multi-sprint variant:
  (string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl,
   decimal BugSp, decimal NonBugSp, decimal CompletedSp, decimal BugRatioPercent,
   int BugTicketCount, int NonBugTicketCount,
   List<BugRatioDeveloperSprintBreakdown> SprintBreakdowns,
   BugRatioAlertStatus Alert)

BugRatioMultiSprintResponse(List<BugRatioSprintInfo> Sprints, BugRatioTeamMetrics TeamMetrics, List<BugRatioIssueTypeEntry> IssueTypeBreakdown, List<BugRatioDeveloperEntry> Developers)

BugRatioMetricCard(string Name, decimal Value, string DisplayValue, decimal? Delta, string? DeltaDirection, string? DeltaPolarity)

BugRatioTeamSingleMetrics(BugRatioMetricCard BugRatioPercent, BugRatioMetricCard BugSp, BugRatioMetricCard NonBugSp)

BugRatioDeveloperDelta(decimal BugSpDelta, string BugSpDeltaDirection, string BugSpDeltaPolarity, decimal NonBugSpDelta, string NonBugSpDeltaDirection, string NonBugSpDeltaPolarity, decimal BugRatioPercentDelta, string BugRatioPercentDeltaDirection, string BugRatioPercentDeltaPolarity, int BugTicketCountDelta, string BugTicketCountDeltaDirection, string BugTicketCountDeltaPolarity, int NonBugTicketCountDelta, string NonBugTicketCountDeltaDirection, string NonBugTicketCountDeltaPolarity)

BugRatioDeveloperSingleEntry:
  (string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl,
   decimal BugSp, decimal NonBugSp, decimal CompletedSp, decimal BugRatioPercent,
   int BugTicketCount, int NonBugTicketCount,
   BugRatioDeveloperDelta? Delta,
   BugRatioAlertStatus Alert)

BugRatioSingleSprintResponse(BugRatioSprintInfo Sprint, BugRatioTeamSingleMetrics TeamMetrics, List<BugRatioIssueTypeEntry> IssueTypeBreakdown, List<BugRatioDeveloperSingleEntry> Developers)

BugRatioResponse(string Mode, BugRatioMultiSprintResponse? MultiSprint, BugRatioSingleSprintResponse? SingleSprint)
```

**Service methods:**
- `ComputeMultiSprint(List<Sprint> targetSprints, List<Sprint> allClosedSprints, List<Developer> activeDevelopers, AppSettings settings, string? subTeam)` — returns `BugRatioMultiSprintResponse`
- `ComputeSingleSprint(Sprint targetSprint, Sprint? priorSprint, List<Sprint> allClosedSprints, List<Developer> activeDevelopers, AppSettings settings, string? subTeam)` — returns `BugRatioSingleSprintResponse`
- Private `EvaluateAlert(string developerId, List<Sprint> allClosedSprints, List<string> doneStatuses, List<string> excludedStatuses, int threshold, int consecutiveCount, string? subTeam)` — returns `BugRatioAlertStatus`

**Key computation logic (business rules from spec):**
- Bug classification: `membership.Ticket?.IssueType == "Bug"` (exact match, case-sensitive — BR1, matches ScopeChangeService pattern)
- Only completed tickets: final status in `doneStatuses` AND `RemovedAt == null` AND not in `excludedStatuses` (BR2, BR4, BR5)
- Bug ratio = Bug SP / Total Completed SP * 100 (BR3); zero completed SP = 0% (BR7)
- Tickets with null StoryPoints excluded from SP metrics but counted in ticket counts (BR6)
- Multi-sprint ratio = total bug SP / total completed SP (ratio of totals, not average of ratios — BR8)
- Alert evaluates all closed sprints from most recent backward (BR9, BR10); zero SP sprint resets streak (BR11)
- Only active developers shown; zero-ticket developers visible with all-zero values (BR12, BR13)
- Sub-team filter applies to developers, not tickets (BR16)
- Sprints ordered by start date ascending (BR17)
- Issue type breakdown: completed tickets only, case-preserved from Jira (BR18)

**Delta polarity (single-sprint mode — BR15):**
- Bug ratio %: positive-down (lower is better)
- Bug SP: positive-down
- Non-bug SP: positive-up (higher is better)
- Bug ticket count: positive-down
- Non-bug ticket count: positive-up

**Pattern to follow:** `ScopeChangeService.cs` for service structure, record hierarchy, metric card builder, sub-team filtering. `DeveloperThroughputService.cs` for per-developer iteration with developer list and membership filtering.

**Follow:** extract-feature-service

**Dependencies:** Step 1 (alert threshold properties on AppSettings)

### Step 3: Create GetBugRatio endpoint

**What:** Create the FastEndpoints GET endpoint with request/validator.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioQuery.cs` — request class + validator
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — endpoint class

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — register `BugRatioService` as scoped

**Request class `GetBugRatioRequest`:**
- `SprintId` (int?, optional)
- `Last` (int?, optional)
- `SubTeam` (string?, optional)

**Validator `GetBugRatioRequestValidator`:**
- SprintId > 0 when present
- Last >= 1 when present
- SprintId and Last mutually exclusive (400 when both provided)
- SubTeam not empty when provided

**Endpoint route:** `GET /api/analytics/bug-ratio`, Tags: "Analytics", AllowAnonymous

**Endpoint `HandleAsync` orchestration (follow GetScopeChangeEndpoint pattern):**
1. Load all closed sprints lightweight via `sprintRepository.GetClosedSprintsAsync(ct)`
2. If none, return empty response with mode "multi"
3. Normalize subTeam
4. Load app settings
5. If `req.SprintId` present (single-sprint mode):
   - Validate sprint exists in closed sprints (400 if not)
   - Find prior sprint by start date
   - Load target + prior sprints with memberships via `GetSprintsWithMembershipsAsync`
   - Load ALL closed sprints with memberships (needed for alert evaluation — BR9)
   - Load active developers
   - Call `bugRatioService.ComputeSingleSprint(...)`
6. Else (multi-sprint mode):
   - Default `last` to 5 when neither param provided
   - Take last N sprints (ascending)
   - Load target sprints with memberships
   - Load ALL closed sprints with memberships (for alert evaluation)
   - Load active developers
   - Call `bugRatioService.ComputeMultiSprint(...)`

**Performance note on alert evaluation:** BR9 requires all closed sprints for alert computation. The endpoint must load all closed sprints with memberships. For optimization, the service can use the already-loaded target sprints and only load the remaining ones for alert-only purposes. However, in v1 with SQLite and small datasets, loading all sprints with memberships is acceptable. If performance becomes an issue, the alert evaluation can be optimized to use a targeted query. The developer should implement the straightforward approach first — load all closed sprints with memberships once, pass both the target subset and the full set to the service.

**Follow:** create-feature (FastEndpoints variant)

**Dependencies:** Step 2 (BugRatioService)

### Step 4: Add frontend TypeScript types

**What:** Add all response type interfaces for the bug ratio API to the shared types file.

**Files to modify:**
- `client/src/types/index.ts` — add Bug Ratio section after Carry-over types

**Types to add (matching the backend response records from Step 2):**
- `BugRatioSprintInfo`
- `BugRatioTeamTrendEntry`
- `BugRatioTeamMetrics`
- `BugRatioIssueTypeEntry`
- `BugRatioAlertStatus`
- `BugRatioDeveloperSprintBreakdown`
- `BugRatioDeveloperEntry`
- `BugRatioMultiSprintResponse`
- `BugRatioMetricCard`
- `BugRatioTeamSingleMetrics`
- `BugRatioDeveloperDelta`
- `BugRatioDeveloperSingleEntry`
- `BugRatioSingleSprintResponse`
- `BugRatioResponse`

Also update the `AppSettings` interface to include `bugRatioAlertThreshold: number` and `bugRatioConsecutiveSprintCount: number`.

**Follow:** create-vue-feature (step 1: define types)

**Dependencies:** None (can be done in parallel with backend steps)

### Step 5: Add bug ratio API function

**What:** Add the `getBugRatio` API function alongside existing analytics functions.

**Files to modify:**
- `client/src/api/analytics.ts` — add `getBugRatio(sprintId?, last?, subTeam?)` function, import `BugRatioResponse` type

**Pattern to follow:** `getScopeChange` function in the same file (identical query param pattern).

**Follow:** create-vue-feature (step 2: API module)

**Dependencies:** Step 4 (types)

### Step 6: Extend developersStore with bug ratio state and tab management

**What:** Add bug ratio state, tab management, and fetch logic to the existing developers store. The bug ratio tab shares sprint selector and sub-team filter state with the throughput tab.

**Files to modify:**
- `client/src/stores/developersStore.ts` — add:
  - `activeTab` ref (`'throughput' | 'bugRatio'`, default `'throughput'`)
  - `bugRatio` ref (`BugRatioResponse | null`)
  - `bugRatioLoading` ref (boolean)
  - `bugRatioError` ref (string | null)
  - `fetchBugRatio()` async action — calls `getBugRatio()` with current sprint/last/subTeam params
  - `switchTab(tab)` action — sets activeTab, fetches bug ratio data on first switch to Bug Ratio tab (lazy load)
  - Modify `selectSprint()`, `selectLastN()`, `selectSubTeam()` to also re-fetch bug ratio data if the Bug Ratio tab is active

**Why extend existing store instead of new store:** The sprint selector, sub-team filter, and closed sprints list are shared state that persists across tabs (BR19). Duplicating this in a separate store adds unnecessary synchronization complexity.

**Follow:** pinia-patterns

**Dependencies:** Steps 4-5 (types and API function)

### Step 7: Restructure DevelopersView with tabs

**What:** Add tab navigation to DevelopersView. The Throughput tab wraps existing content. The Bug Ratio tab delegates to a new `BugRatioTab` container component. Sprint selector and sub-team filter remain in PageToolbar, shared across tabs.

**Files to modify:**
- `client/src/views/DevelopersView.vue` — add tab bar UI (two buttons: Throughput, Bug Ratio), conditionally render existing throughput content vs new `BugRatioTab` component based on `store.activeTab`. Throughput remains default active tab.

**Files to create:**
- `client/src/components/developers/BugRatioTab.vue` — L1 container component that receives bug ratio data as props and renders the sub-components (metric cards, charts, table, issue type breakdown). Handles multi vs single sprint mode switching.

**Tab bar pattern:** Simple inline tab buttons using Tailwind classes, matching the app's existing design language. Active tab gets accent border/text, inactive gets muted text.

**URL sync:** Add `tab` query parameter to URL sync logic (`?tab=bug-ratio`). Default (no param or `throughput`) shows Throughput tab.

**Follow:** vue-component-architecture (L0 view with L1 container)

**Dependencies:** Step 6 (store with tab management)

### Step 8: Create Bug Ratio tab components

**What:** Create the visual components for the Bug Ratio tab content.

**Files to create:**
- `client/src/components/developers/BugRatioMetricCards.vue` — L3 props-only component. Displays 3 summary cards: Team Bug Ratio %, Total Bug SP, Total Non-Bug SP. In single-sprint mode, shows delta badges with polarity coloring.
- `client/src/components/developers/BugRatioTrendChart.vue` — L3 props-only component. ApexCharts line chart showing team bug ratio % per sprint across the selected range. Multi-sprint mode only.
- `client/src/components/developers/BugRatioStackedChart.vue` — L3 props-only component. ApexCharts stacked bar chart showing Bug SP (red) vs Non-Bug SP (blue) per sprint per developer. Multi-sprint mode only.
- `client/src/components/developers/BugRatioDevTable.vue` — L3 props-only component. Per-developer table with columns: name (with avatar), sub-team, bug SP, non-bug SP, bug ratio %, bug ticket count, non-bug ticket count. Alert badge (warning icon) on developers with active alert. In single-sprint mode, shows deltas with polarity coloring. In multi-sprint mode, shows totals.
- `client/src/components/developers/BugRatioIssueTypeBreakdown.vue` — L3 props-only component. Table or list showing completed ticket counts by raw Jira issue type with SP totals.

**Chart colors:**
- Bug SP: red (matching spec — use `#ef4444` / Tailwind red-500)
- Non-Bug SP: blue (matching spec — use `#3b82f6` / Tailwind blue-500)
- Bug ratio trend line: use accent color

**Alert badge:** Warning icon (triangle exclamation) with tooltip showing "Bug ratio above {threshold}% for {N} consecutive sprints"

**Delta styling:** Reuse the `deltaIcon` and `deltaClass` helper pattern from DevelopersView (already exists for throughput deltas).

**Follow:** vue-patterns, vue-component-architecture

**Dependencies:** Step 7 (BugRatioTab container)

### Step 9: Update frontend settings form

**What:** Add bug ratio alert threshold inputs to the Settings page.

**Files to modify:**
- `client/src/views/SettingsView.vue` — add a "Bug Ratio Alerts" section with two inputs: threshold percentage (0-100, default 50) and consecutive sprint count (1-10, default 2). Place after the Health Weights section.
- `client/src/stores/settingsStore.ts` — ensure the store maps the new `bugRatioAlertThreshold` and `bugRatioConsecutiveSprintCount` fields from/to the API

**Pattern to follow:** Existing health threshold inputs in SettingsView.vue.

**No skill — straightforward field additions.**

**Dependencies:** Step 4 (AppSettings type update)

### Step 10: Build verification and integration test

**What:** Verify the full build passes and test the feature end-to-end.

**Backend:**
```
dotnet build src/Services/Fokus/Fokus.API
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Frontend:**
```
cd client && npm run build
```

**Manual verification:**
- Start the dev server
- Navigate to Developers page — verify tabs appear (Throughput default)
- Click Bug Ratio tab — verify data loads
- Switch sprint selector — verify both tabs respond
- Check single-sprint mode — verify deltas appear
- Check alert badges on developers exceeding threshold
- Verify Settings page shows new alert threshold fields
- Save settings and verify they persist

**Dependencies:** All previous steps

## Cross-Service Changes

None. Fokus is a single-service system with no cross-service dependencies in v1.

## Migration Notes

```bash
dotnet ef migrations add AddBugRatioAlertSettings -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data changes needed — the new columns have defaults (50, 2) and the existing singleton row will get these defaults via the migration.

## Testing Strategy

**Backend:**
- GET /api/analytics/bug-ratio with no params returns last 5 closed sprints data
- GET with `sprintId` returns single-sprint detail with deltas
- GET with `last=3` returns 3-sprint data
- 400 when both `sprintId` and `last` provided
- 400 when `sprintId` doesn't match a closed sprint
- 400 when `last` < 1
- Bug ratio calculation: verify Bug SP / Total SP * 100
- Zero completed SP produces 0% (not null/error)
- Multi-sprint ratio uses totals, not averages
- Alert fires only after N consecutive sprints above threshold
- Zero SP sprint resets consecutive streak
- Inactive developers excluded
- Active developers with zero tickets appear with all-zero values
- Sub-team filter scopes all data
- Removed tickets excluded
- Excluded-from-scope statuses excluded
- Null SP tickets excluded from SP metrics but counted in ticket counts

**Frontend:**
- Tab switching persists sprint/sub-team selection
- Throughput is default tab
- Bug Ratio tab lazy-loads data on first activation
- Delta polarity: green down for bug ratio/bug SP, green up for non-bug SP
- Alert badge appears for qualifying developers
- Settings form saves and loads new threshold values

## Open Questions

None.
