# Developer Detail Page

**Feature Spec:** `docs/specs/F34-DeveloperDetailPage/definition/spec.md`

## Context

The Developers page shows team-level analytics across six tabs. A Scrum Master preparing for a 1:1 has no single place to see an individual developer's full picture: velocity trends across sprints, work type allocation (feature vs bug), and current sprint ticket-level detail. The Developer Detail Page provides this individual lens, composing data from existing analytics patterns (Leaderboard's bug/feature SP split, DeveloperProgress's current sprint data, Throughput's rolling average) into a focused view for coaching and mid-sprint course correction.

**Services impacted:** Fokus (single service). Backend: new analytics service + endpoint, new settings field + save endpoint. Frontend: new view, store, route, chart components, settings subsection, navigation links from Daily Progress cards.

## Scope

**In scope:**
- Backend: `DeveloperDetailService` computing cross-sprint trends and current sprint detail for a single developer
- Backend: `GetDeveloperDetailEndpoint` at `GET /api/analytics/developer-detail/{accountId}`
- Backend: `BugRatioTarget` field on `AppSettings` (decimal, default 30)
- Backend: `PUT /api/settings/analytics-targets` save endpoint
- Backend: Extend `GET /api/settings` response with `bugRatioTarget`
- Frontend: New `/developers/:accountId` route and `DeveloperDetailView`
- Frontend: `developerDetailStore` with fetch action
- Frontend: Sprint Trends stacked bar chart, Work Allocation line chart, full-width Burnup chart, Ticket table
- Frontend: Sprint range selector (Last 5 / Last 10 / All)
- Frontend: Navigation links from `DeveloperProgressCard` to detail page
- Frontend: "Analytics Targets" subsection in the Sync settings tab
- Frontend: Breadcrumb navigation back to Developers page (Daily Progress tab)

**Out of scope:** Per-developer bug ratio targets, git metrics, AI adoption metrics, developer comparison mode, cycle time per developer, print/export.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | Add `BugRatioTarget` field to `AppSettings`, update EF config, migration | |
| 2 | create-feature | Follow | `SaveAnalyticsTargets` endpoint, `GetSettings` response extension | |
| 3 | (none) | -- | `DeveloperDetailService` — cross-sprint trend computation, current sprint detail, ticket state derivation | Log: metric computation patterns gap |
| 4 | create-feature | Follow | `GetDeveloperDetail` endpoint with route param + query param | |
| 5 | create-vue-feature | Follow | Types, API function, store, route, view for developer detail | |
| 6 | vue-patterns, vue-component-architecture | Follow | Sprint Trends stacked bar chart component | |
| 7 | vue-patterns, vue-component-architecture | Follow | Work Allocation line chart + Current Sprint section components | |
| 8 | vue-patterns | Follow | Ticket table component with Jira links, state grouping | |
| 9 | vue-patterns | Follow | Navigation links from DeveloperProgressCard, breadcrumb | |
| 10 | vue-patterns | Follow | Analytics Targets subsection in SyncTab | |

## Domain Model Changes

No new entities, value objects, or domain events.

One new field on `AppSettings`:
- `BugRatioTarget` (decimal, default 30) — team-wide bug ratio target percentage (0-100)

## Data Model Changes

- New column `BugRatioTarget` (decimal) on `AppSettings` table, default 30
- EF migration required

## Implementation Steps

### Step 1: Add BugRatioTarget to AppSettings and create migration

Add the `BugRatioTarget` field to the domain entity, update the EF configuration, update the settings response, and generate the migration.

**Follow persistence-patterns.**

**Files:**
- Modify: `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`
- Modify: `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` (add `BugRatioTarget` to `GetSettingsResponse`)
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` (map new field to response)
- Create: new migration file via `dotnet ef migrations add AddBugRatioTarget`

**Feature-specific inputs:**
- Property: `public decimal BugRatioTarget { get; set; } = 30;`
- Add to `CreateDefault()` factory: `BugRatioTarget = 30`
- No JSON column, no value comparer — simple decimal scalar
- `GetSettingsResponse`: add `public decimal BugRatioTarget { get; set; }`
- `GetSettingsEndpoint.HandleAsync`: map `settings.BugRatioTarget` to response

**Accept:**
- `AppSettings.BugRatioTarget` exists with default 30
- Migration runs cleanly, existing data gets default value
- `GET /api/settings` response includes `bugRatioTarget`

**Dependencies:** None

---

### Step 2: Create SaveAnalyticsTargets endpoint

Create the settings save endpoint following the per-section save pattern.

**Follow create-feature.**

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Settings/SaveAnalyticsTargets/SaveAnalyticsTargetsEndpoint.cs`

**Feature-specific inputs:**
- Route: `PUT /api/settings/analytics-targets`
- Tags: `"Settings"`
- Auth: `[Authorize(Roles = "Admin")]`
- Request: `SaveAnalyticsTargetsRequest` with `BugRatioTarget` (decimal)
- Validator: `BugRatioTarget` required, `InclusiveBetween(0, 100)`
- Handler: load settings via `AppSettingsRepository.GetAsync`, set `BugRatioTarget`, save, return `SendOkAsync(new { Success = true })`
- Pattern reference: `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsEndpoint.cs` — identical structure (request + validator + endpoint + response in one file)
- **Spec divergence (acknowledged):** Spec says 204 no-content, but every existing settings save endpoint returns 200 with `{ success: true }` body. Follow the codebase pattern (200) for frontend consistency — the settings store already handles `{ success: true }` responses.

**Accept:**
- `PUT /api/settings/analytics-targets` with `{ bugRatioTarget: 25 }` returns 200 with `{ success: true }`
- Returns 400 for values outside 0-100
- Only Admin role can save

**Dependencies:** Step 1

---

### Step 3: Create DeveloperDetailService

Create the backend computation service that produces the full developer detail response: cross-sprint trends (feature/bug SP split, completion %, rolling average, bug %), work allocation summary, and current sprint detail (burnup, tickets with state derivation).

**No matching skill** -- metric computation patterns are a gap.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperDetailService.cs`

**Record shapes (defined in the same file, above the service class):**

Top-level response:
- `DeveloperDetailResponse(DeveloperDetailInfo Developer, List<SprintTrendEntry> SprintTrends, WorkAllocationSummary WorkAllocation, CurrentSprintDetail? CurrentSprint, decimal BugRatioTarget)`

Developer info:
- `DeveloperDetailInfo(string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl, string Role, int DefaultCapacityPercent)`

Per-sprint trend:
- `SprintTrendEntry(int SprintId, string SprintName, DateTime StartDate, DateTime EndDate, decimal FeatureSp, decimal BugSp, decimal TotalSp, decimal AssignedSp, decimal CompletionPercent, int CapacityPercent, decimal? RollingAverageSp, decimal BugPercent)`

Work allocation:
- `WorkAllocationSummary(decimal AverageBugPercent, int SprintsAboveTarget, int TotalSprints)`

Current sprint:
- `CurrentSprintDetail(DeveloperProgressSprintInfo Sprint, int CurrentDay, int TotalDays, decimal AssignedSp, decimal CompletedSp, decimal CompletionPercent, decimal FeatureCompletedSp, decimal BugCompletedSp, decimal DailyPace, bool IsBehindPace, decimal? PaceGapSp, List<DayBreakdownEntry> DailyBreakdown, List<TicketDetailEntry> Tickets)`

Per ticket:
- `TicketDetailEntry(string Key, string Summary, string CurrentStatus, decimal? StoryPoints, string IssueType, int DaysInCurrentStatus, string State, bool IsStalled)`

**Computation rules (domain constraints for the developer — not method-body logic):**

Cross-sprint trends:
- Bug classification: `IssueType == "Bug"` (exact, case-sensitive) — same as `LeaderboardService` line 287
- Completed filter: transition-based via `TransitionAttributionChecker.IsCompletedInSprint`, exclude removed memberships, exclude `ExcludedFromScopeStatuses` — same as `LeaderboardService.GetTransitionCompletedMemberships` (line 242)
- AssignedSp: all non-removed memberships, all ticket types (not feature-only) — `m.RemovedAt == null`, sum `GetEffectiveSp(defaultSpPerBug)`
- CompletionPercent: `totalSp / assignedSp * 100` (0 when assignedSp == 0). All-types, not feature-only.
- Rolling average: 3-sprint rolling average of `totalSp`. Null for first 2 sprints in the series. Walk backward, skip sprints with 0% capacity — same as `DeveloperThroughputService` rolling average but over all-types SP.
- BugPercent: `bugSp / totalSp * 100` (0 when totalSp == 0)
- Capacity: same resolution as `LeaderboardService.GetCapacity` (sprint override then developer default)
- Sprint inclusion: only sprints where developer has at least one non-removed membership. Exclude sprints where capacity == 0 AND totalSp == 0.
- Sprint ordering: chronological (oldest first)
- Sprint range: `last` parameter controls count. Default 10. `last=0` means all. Range counts only sprints passing inclusion rule.

Work allocation summary:
- `averageBugPercent`: average of `bugPercent` across displayed sprints, excluding sprints with totalSp == 0
- `sprintsAboveTarget`: count where `bugPercent > bugRatioTarget`
- `totalSprints`: count of displayed sprints

Current sprint detail:
- Reuse the same pace/burnup computation as `DeveloperProgressService` (lines 66-216) — totalDays, currentDay, dailyPace, completionPercent, dailyBreakdown, stall detection, feature/bug SP split
- Ticket state derivation per spec BR10: `done` (completed via IsCompletedInSprint), `stalled` (started + not completed + no transition in 2+ business days), `in-progress` (started + not completed + not stalled), `not-started` (not started via IsStartedInSprint)
- DaysInCurrentStatus: business days (Mon-Fri) since last transition. For tickets never transitioned: count from sprint start date. Reuse `CountBusinessDays` pattern from `DeveloperProgressService` line 367.
- Ticket ordering: stalled first (most days stalled desc), then in-progress, then not-started, then done. Within each group: story points descending (largest first).

**Pattern references:**
- `LeaderboardService.cs` — bug/feature SP split, completed filter with ExcludedFromScopeStatuses, capacity resolution
- `DeveloperProgressService.cs` — pace computation, stall detection, business day counting, feature/bug SP split on completed
- `DeveloperThroughputService.cs` — rolling average computation (capacity-aware skip)
- `TransitionAttributionChecker.cs` — started/completed checks

**Accept:**
- Service class with a `ComputeDetail` method accepting loaded data (sprint, developers, capacities, settings, transitions) and returning `DeveloperDetailResponse`
- Cross-sprint featureSp/bugSp match Leaderboard values for same developer and sprint (same scope model)
- CompletionPercent is all-types (diverges from Throughput's feature-only by design)
- Rolling average skips 0-capacity sprints, null for first 2
- Current sprint section uses same pace/burnup logic as DeveloperProgressService
- Ticket state derivation matches spec BR10 definitions
- Ticket ordering matches spec BR12

**Confidence:** Medium — composing patterns from 3 existing services into one; developer should read all three before implementing.

**Dependencies:** Step 1 (BugRatioTarget on AppSettings)

---

### Step 4: Create GetDeveloperDetail endpoint

Create the analytics endpoint that loads data and delegates to `DeveloperDetailService`.

**Follow create-feature.**

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs`
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailQuery.cs`

**Feature-specific inputs:**
- Route: `GET /api/analytics/developer-detail/{accountId}`
- Tags: `"Analytics"`
- Request: `GetDeveloperDetailRequest` with `AccountId` (string, route param) and `Last` (int?, query param, default 10)
- Injected dependencies: `SprintRepository`, `DeveloperRepository`, `AppSettingsRepository`, `TicketRepository`, `DeveloperDetailService`, `IOptions<JiraOptions>`
- Handler logic flow:
  1. Look up developer by `req.AccountId` in all developers. If not found or not active, return 404.
  2. Apply excluded developer check — if excluded (0% capacity + 0 completed in all sprints), return 404.
  3. Load analytics sprints, filter to closed sprints with this developer's memberships.
  4. Load sprints with memberships (via `GetSprintsWithMembershipsAsync`), capacities, settings, transitions.
  5. Build capacity lookup (same pattern as `GetLeaderboardEndpoint` lines 42-49).
  6. Compute current sprint detail: find active sprint, load with memberships if exists.
  7. Call `DeveloperDetailService.ComputeDetail(...)`.
  8. Attach `jiraInstanceUrl` from `IOptions<JiraOptions>` to the response (needed for frontend Jira links). Add `JiraInstanceUrl` (string) to `DeveloperDetailResponse`.
  9. Return 200 with response.
- Pattern reference: `GetLeaderboardEndpoint.cs` — same data loading pattern (analytics sprints, memberships, capacities, transitions, excluded developer filter)

**Accept:**
- Returns 200 with full `DeveloperDetailResponse`
- Returns 404 for unknown or excluded developers
- `?last=5`, `?last=10`, `?last=0` filter sprint range correctly
- Response includes `bugRatioTarget` from settings and `jiraInstanceUrl` from JiraOptions

**Dependencies:** Step 3

---

### Step 5: Frontend types, API function, store, route, and view skeleton

Create the complete frontend vertical slice: TypeScript types, API module function, Pinia store, route entry, and view component.

**Follow create-vue-feature.** Also follow **pinia-patterns** for the store.

**Files:**
- Modify: `client/src/types/index.ts` — add `DeveloperDetailResponse` and child types
- Modify: `client/src/api/analytics.ts` — add `getDeveloperDetail(accountId, last?)` function
- Create: `client/src/stores/developerDetailStore.ts` — store with fetch action
- Modify: `client/src/router.ts` — add `/developers/:accountId` route
- Create: `client/src/views/DeveloperDetailView.vue` — L0 page component

**Feature-specific inputs:**

Types (add to `client/src/types/index.ts`):
- `DeveloperDetailInfo` — accountId, displayName, subTeam, avatarUrl, role, defaultCapacityPercent
- `SprintTrendEntry` — sprintId, sprintName, startDate, endDate, featureSp, bugSp, totalSp, assignedSp, completionPercent, capacityPercent, rollingAverageSp (number | null), bugPercent
- `WorkAllocationSummary` — averageBugPercent, sprintsAboveTarget, totalSprints
- `TicketDetailEntry` — key, summary, currentStatus, storyPoints (number | null), issueType, daysInCurrentStatus, state (`'stalled' | 'in-progress' | 'done' | 'not-started'`), isStalled
- `CurrentSprintDetail` — sprint (DeveloperProgressSprintInfo), currentDay, totalDays, assignedSp, completedSp, completionPercent, featureCompletedSp, bugCompletedSp, dailyPace, isBehindPace, paceGapSp (number | null), dailyBreakdown (DayBreakdownEntry[]), tickets (TicketDetailEntry[])
- `DeveloperDetailResponse` — developer (DeveloperDetailInfo), sprintTrends (SprintTrendEntry[]), workAllocation (WorkAllocationSummary), currentSprint (CurrentSprintDetail | null), bugRatioTarget (number), jiraInstanceUrl (string)

API function: `getDeveloperDetail(accountId: string, last?: number): Promise<DeveloperDetailResponse>` — follows existing analytics API pattern with URLSearchParams.

Store (`developerDetailStore`):
- State: `data` (ref, `DeveloperDetailResponse | null`), `loading`, `error`, `selectedLast` (ref, default 10)
- Actions: `fetchDetail(accountId: string)` — calls `getDeveloperDetail(accountId, selectedLast)`, `setSprintRange(last: number)` — updates `selectedLast` and re-fetches

Route: `{ path: '/developers/:accountId', name: 'developer-detail', component: () => import('./views/DeveloperDetailView.vue') }` — add before the catch-all or after the `/developers` route.

View (`DeveloperDetailView.vue`, L0 page component):
- Read `accountId` from `useRoute().params.accountId`
- Fetch on mount via store action
- Show loading state, error state, "Developer not found" state (404)
- Breadcrumb: "Developers > {Developer Name}" — link to `/developers?tab=daily-progress`
- Sprint range selector (Last 5 / Last 10 / All) — drives `store.setSprintRange()`
- Render child components: developer header, SprintTrendsChart, WorkAllocationChart, CurrentSprintSection
- Pattern reference: `DevelopersView.vue` — similar structure (store init, conditional rendering)

**Accept:**
- Types match backend response shape exactly
- API function handles `last` query parameter
- Store manages loading/error state
- Route `/developers/:accountId` navigable (deep link works)
- View shows breadcrumb, loading state, error state, developer-not-found
- Sprint range selector updates both charts simultaneously

**Dependencies:** Step 4 (backend must exist for API contract)

---

### Step 6: Sprint Trends stacked bar chart component

Create the stacked bar chart showing feature SP (blue) and bug SP (red) per sprint with completion % trend line and rolling average line.

**Follow vue-patterns, vue-component-architecture.**

**Files:**
- Create: `client/src/components/developer-detail/SprintTrendsChart.vue` (L3 humble component, props-only)

**Feature-specific inputs:**
- Props: `sprintTrends: SprintTrendEntry[]`
- ApexCharts stacked bar chart with two bar series (Feature SP blue, Bug SP red) and two line series on secondary Y-axis (Completion % 0-100%, Rolling Average SP)
- X-axis: sprint names
- Primary Y-axis: SP values
- Secondary Y-axis: completion % (0-100%)
- Tooltip on bar hover: sprint name, feature SP, bug SP, total SP, completion %, capacity %
- Colors: feature SP = blue (#3b82f6), bug SP = red (#ef4444), completion line = gray, rolling average = dashed
- Chart type: mixed (bar + line) via ApexCharts series configuration
- Pattern reference: `DeveloperProgressCard.vue` — ApexCharts configuration pattern (options computed, series computed)

**Accept:**
- Stacked bar chart renders feature SP (blue) and bug SP (red) per sprint
- Completion % trend line on secondary Y-axis (0-100%) labeled "(all types)" to signal divergence from Throughput's feature-only %
- Rolling average line visible (null values handled — gaps for first 2 sprints)
- Hover tooltip shows all 6 fields per spec Flow 2.6
- Chart responsive at full width

**Dependencies:** Step 5 (types and view must exist)

---

### Step 7: Work Allocation chart and Current Sprint section

Create the work allocation line chart (bug % per sprint with target reference line) and the current sprint section (full-width burnup chart + SP summary).

**Follow vue-patterns, vue-component-architecture.**

**Files:**
- Create: `client/src/components/developer-detail/WorkAllocationChart.vue` (L3 humble component)
- Create: `client/src/components/developer-detail/CurrentSprintSection.vue` (L1 feature component)

**Feature-specific inputs:**

WorkAllocationChart:
- Props: `sprintTrends: SprintTrendEntry[]`, `workAllocation: WorkAllocationSummary`, `bugRatioTarget: number`
- Line chart showing bug % per sprint
- Horizontal dashed reference line (ApexCharts `yaxis.annotations`) at `bugRatioTarget`
- Points where bugPercent > bugRatioTarget visually highlighted (red marker or different color)
- Tooltip: sprint name, bug SP, feature SP, bug %, target %
- Summary line below chart: "Average bug %: X.X% | Y of Z sprints above target"
- X-axis: sprint names (same as Sprint Trends chart — they share the same data range)

CurrentSprintSection:
- Props: `currentSprint: CurrentSprintDetail | null`, `jiraInstanceUrl: string`
- If `currentSprint` is null: show "No active sprint" message
- If present:
  - Full-width burnup chart (same series as DeveloperProgressCard but larger — height ~300px): completed SP line vs expected pace line
  - SP summary: `completed / assigned SP (completion%) (all types) — X features / Y bugs` — the "(all types)" label is required per spec BR14 to signal this includes bugs in the denominator
  - Render `TicketTable` child component
- Pattern reference: `DeveloperProgressCard.vue` — burnup chart series and options (scale up, not mini)

**Accept:**
- Bug % line chart with dashed target reference line
- Points above target visually highlighted
- Summary line shows average bug % and sprint count above target
- Current sprint section hidden when null, "No active sprint" message
- Burnup chart at full width with clearly distinguishable lines
- SP summary with feature/bug split

**Dependencies:** Steps 5, 8 (TicketTable component)

---

### Step 8: Ticket table component

Create the ticket table showing all assigned tickets in the current sprint, grouped by state with Jira links.

**Follow vue-patterns.**

**Files:**
- Create: `client/src/components/developer-detail/TicketTable.vue` (L3 humble component)

**Feature-specific inputs:**
- Props: `tickets: TicketDetailEntry[]`, `jiraInstanceUrl: string`
- Columns: Key (linked), Summary, Status, SP, Issue Type, Days in Status
- Key column: `<a :href="`${jiraInstanceUrl}/browse/${ticket.key}`" target="_blank">{{ ticket.key }}</a>` — uses `jiraInstanceUrl` from the API response (BR13: from Jira connection config, not AppSettings)
- Grouped display: render tickets in state order (stalled, in-progress, not-started, done) — API pre-sorts, frontend renders in order
- State group headers or visual separators between groups
- Stalled tickets: warning indicator (amber/orange border or icon), show days since last transition prominently
- Within each group: sorted by SP descending (API pre-sorts)
- Null SP: display as "—"

**Accept:**
- Table renders all tickets grouped by state
- Ticket keys link to Jira (opens in new tab)
- Stalled tickets have visual warning indicator
- Ordering matches spec BR12
- Null SP displayed as dash
- Responsive — horizontal scroll on narrow viewports if needed

**Dependencies:** Step 5 (types must exist)

---

### Step 9: Navigation links from DeveloperProgressCard and breadcrumb

Add clickable links on the Daily Progress cards to navigate to the Developer Detail page, and ensure breadcrumb navigation works.

**Follow vue-patterns.**

**Files:**
- Modify: `client/src/components/developers/DeveloperProgressCard.vue` — wrap developer name and avatar in `<router-link>`

**Feature-specific inputs:**
- Wrap the developer name (`<div class="text-sm font-medium ...">`) and avatar (`<img>` / fallback `<div>`) in a `<router-link :to="{ name: 'developer-detail', params: { accountId: developer.accountId } }">` — or wrap the entire header row
- Add hover styling to indicate clickability (underline on name, cursor pointer)
- Breadcrumb in `DeveloperDetailView.vue` (already created in Step 5): link text "Developers" navigates to `/developers?tab=daily-progress`, then " > {Developer Name}" (not a link)
- Back button (browser native) should return to Daily Progress tab — ensured by the query param `?tab=daily-progress` on the Developers link

**Accept:**
- Clicking developer name or avatar on Daily Progress card navigates to `/developers/{accountId}`
- Breadcrumb shows "Developers > {Developer Name}"
- "Developers" link in breadcrumb goes to Developers page with Daily Progress tab active
- Hover state on name/avatar indicates clickability

**Dependencies:** Step 5 (route must exist)

---

### Step 10: Analytics Targets subsection in Sync settings tab

Add the bug ratio target configuration field as a subsection within the existing Sync settings tab.

**Follow vue-patterns.**

**Files:**
- Modify: `client/src/types/index.ts` — add `bugRatioTarget: number` to `AppSettings` interface
- Modify: `client/src/api/settings.ts` — add `saveAnalyticsTargets(bugRatioTarget: number)` function
- Modify: `client/src/stores/settingsStore.ts` — add `saveAnalyticsTargetsAction`, update default state with `bugRatioTarget: 30`
- Modify: `client/src/views/SettingsView.vue` — add `bugRatioTarget` to `form` reactive object and `syncFromStore()`
- Modify: `client/src/components/settings/SyncTab.vue` — add "Analytics Targets" subsection

**Feature-specific inputs:**

AppSettings type: add `bugRatioTarget: number` (after `defaultSpPerBug`)

API function: `saveAnalyticsTargets(bugRatioTarget: number): Promise<{ success: boolean }>` — `PUT /api/settings/analytics-targets` with `{ bugRatioTarget }`

Store action: `saveAnalyticsTargetsAction(bugRatioTarget: number)` — same pattern as `saveBugRatioAlertsAction`

SyncTab subsection (add after the "Sync Custom Range" section, as a new `<section>` block):
- Section title: "Analytics Targets"
- Info tooltip: "Configure target thresholds used as reference lines in developer analytics charts."
- Field: "Bug Ratio Target (%)" — number input, min 0, max 100, step 1
- Save button: "Save Analytics Targets" — calls `store.saveAnalyticsTargetsAction(form.bugRatioTarget)`
- Success/error feedback — same pattern as `saveSyncConfigPanel()`
- Read-only guard: disabled when `isReadOnly` (non-Admin)

SettingsView form: add `bugRatioTarget: 30` to initial state and `syncFromStore()`

**Accept:**
- "Analytics Targets" subsection visible in Sync tab
- "Bug Ratio Target (%)" field with default 30
- Save persists via `PUT /api/settings/analytics-targets`
- Validation: 0-100 range enforced (backend returns 400)
- Read-only for non-Admin users
- Saving updates the store's `settings.bugRatioTarget` locally

**Dependencies:** Steps 1-2 (backend settings must exist)

---

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

Run after Step 1:
```
cd src/Services/Fokus/Fokus.Persistence
dotnet ef migrations add AddBugRatioTarget --startup-project ../Fokus.API
```

The migration adds a `BugRatioTarget` decimal column with default value 30 to the `AppSettings` table. Existing rows get the default.

## Testing Strategy

### Backend — DeveloperDetailService
- Developer with mixed bugs and features across sprints: verify featureSp/bugSp match Leaderboard values for same sprints
- Developer with 0 completed SP in a sprint: completionPercent = 0, bugPercent = 0
- Developer absent from a sprint (0% capacity + 0 tickets): sprint excluded from trends
- Rolling average: null for first 2 sprints, correct 3-sprint average after, skips 0-capacity sprints
- Sprint range: `last=5` returns last 5 qualifying sprints, `last=0` returns all
- Work allocation averageBugPercent excludes 0-totalSp sprints from average
- sprintsAboveTarget counts correctly against configured bugRatioTarget

### Backend — Current Sprint Detail
- Ticket state derivation: done (completed), stalled (started + not completed + 2+ business days), in-progress (started + not completed + not stalled), not-started (not started)
- DaysInCurrentStatus: business days only (Mon-Fri), from last transition or sprint start
- Ticket ordering: stalled first (most days desc), then in-progress, then not-started, then done; within group by SP desc
- No active sprint: currentSprint is null, 200 response (not 404)

### Backend — Endpoint
- Unknown accountId: 404
- Excluded developer (0% capacity + 0 completed): 404
- Valid developer: 200 with full response including jiraInstanceUrl and bugRatioTarget
- `?last=5`, `?last=10`, `?last=0` filter correctly

### Backend — Settings
- `PUT /api/settings/analytics-targets` with valid value: 200
- Invalid value (< 0 or > 100): 400
- Non-Admin: forbidden
- `GET /api/settings` includes bugRatioTarget

### Frontend — Charts
- Sprint Trends: stacked bars (blue feature, red bug), completion % line, rolling average line
- Work Allocation: line chart with dashed target line, highlighted points above target
- Burnup: full-width, actual vs expected clearly visible
- Sprint range selector updates both Sprint Trends and Work Allocation charts

### Frontend — Ticket Table
- Jira links open correct URL in new tab
- State groups visible with correct ordering
- Stalled tickets have warning indicator
- Null SP shows dash

### Frontend — Navigation
- Click developer name/avatar on Daily Progress card: navigates to `/developers/{accountId}`
- Breadcrumb "Developers" link: goes to `/developers?tab=daily-progress`
- Direct URL navigation works (deep link)
- Unknown developer shows "Developer not found" with link back

### Frontend — Settings
- Analytics Targets subsection in Sync tab
- Bug Ratio Target field with default 30, validation 0-100
- Save persists and updates store

## KB Impact

- Update: `docs/kb/domain/settings.md` — add `BugRatioTarget` to properties table
- Create: `docs/kb/analytics/developer-detail.md` — scope model (all-types, Leaderboard-pattern completed filter), sprint inclusion rule, ticket state derivation, rolling average (all-types), relationship to Throughput/Leaderboard/DailyProgress

## Open Questions

None — all resolved during analysis (Q1 answered: Analytics Targets goes as subsection in Sync tab).
