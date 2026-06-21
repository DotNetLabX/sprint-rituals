# F34-DeveloperDetailPage — Implementation

## Status: COMPLETE

## Steps

### Step 1: Add BugRatioTarget to AppSettings and create migration
**Status:** Done

**Files Modified:**
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added `public decimal BugRatioTarget { get; set; } = 30` property and `BugRatioTarget = 30` to `CreateDefault()` factory method.
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — No changes needed; EF Core auto-maps simple scalar decimal properties from the base `EntityConfiguration<T>`.
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Added `public decimal BugRatioTarget { get; set; }` to `GetSettingsResponse`.
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Added `BugRatioTarget = settings.BugRatioTarget` mapping.
- `src/Services/Fokus/Fokus.Persistence/Migrations/` — Created `AddBugRatioTarget` migration via `dotnet ef migrations add AddBugRatioTarget --startup-project ../Fokus.API`.

**Build:** Passed.

---

### Step 2: Create SaveAnalyticsTargets endpoint
**Status:** Done

**Files Created:**
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveAnalyticsTargets/SaveAnalyticsTargetsEndpoint.cs` — Created following `SaveBugRatioAlertsEndpoint.cs` pattern. Contains `SaveAnalyticsTargetsRequest`, `SaveAnalyticsTargetsRequestValidator` (BugRatioTarget InclusiveBetween 0–100), `SaveAnalyticsTargetsResponse`, and `SaveAnalyticsTargetsEndpoint`. Route: `PUT /api/settings/analytics-targets`, Tags: "Settings", Auth: Admin only. Returns 200 `{ Success = true }` per codebase convention (spec said 204, but every existing save endpoint returns 200 with body).

**Build:** Passed.

---

### Step 3: Create DeveloperDetailService
**Status:** Done

**Files Created:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperDetailService.cs` — Created with all record types above the service class: `DeveloperDetailInfo`, `SprintTrendEntry`, `WorkAllocationSummary`, `CurrentSprintDetail`, `TicketDetailEntry`, `DeveloperDetailResponse` (includes `JiraInstanceUrl` string for Step 4).

**Key decisions:**
- Rolling average is computed over all-types SP (not feature-only like `DeveloperThroughputService`), as specified.
- Ticket state derivation: `done` (IsCompletedInSprint), `stalled` (IsStartedInSprint + not completed + businessDays > 2), `in-progress` (started + not completed + not stalled), `not-started` (not started).
- `DaysInCurrentStatus` uses `CountBusinessDays` from sprint start date for tickets with no transitions.
- Sprint inclusion: skip sprints where capacity == 0 AND totalSp == 0.
- Variable name collision fixed: `out var dayCompletions` (renamed from `out var tickets`) to avoid CS0136 conflict with `var tickets = BuildTicketDetails(...)` in same scope.

**Build:** Passed.

---

### Step 4: Create GetDeveloperDetail endpoint
**Status:** Done

**Files Created:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailQuery.cs` — `GetDeveloperDetailRequest` with `AccountId` (route param) and `Last` (nullable int query param).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs` — Route: `GET /api/analytics/developer-detail/{accountId}`. Loads developer (404 if missing/inactive), applies excluded developer check, loads closed sprints with memberships, capacity lookup, settings, transitions, active sprint. Calls `DeveloperDetailService.ComputeDetail(...)`. Returns 200.

**Deviations:**
- Added explicit `using Jira.RestApi;` and `using Microsoft.Extensions.Options;` — `JiraOptions` is not in GlobalUsings, pattern confirmed from `SyncXrayEndpoint.cs`.

**Build:** Passed (required building Persistence first to clear MSBuild stale cache; this is a known local toolchain issue unrelated to code correctness).

---

### Step 5: Frontend types, API function, store, route, and view skeleton
**Status:** Done

**Files Modified/Created:**
- `client/src/types/index.ts` — Added 6 interfaces before "Daily Developer Progress types": `DeveloperDetailInfo`, `SprintTrendEntry`, `WorkAllocationSummary`, `TicketDetailEntry`, `CurrentSprintDetail`, `DeveloperDetailResponse`. `CurrentSprintDetail` references `DeveloperProgressSprintInfo` and `DayBreakdownEntry` (defined later in the file — valid in TypeScript).
- `client/src/api/analytics.ts` — Added `getDeveloperDetail(accountId, last?)` using `URLSearchParams` for optional `last` param.
- `client/src/stores/developerDetailStore.ts` — Created setup store with `data`, `loading`, `error`, `selectedLast` (default 10) refs; `fetchDetail`, `setSprintRange`, `reset` actions.
- `client/src/router.ts` — Added `{ path: '/developers/:accountId', name: 'developer-detail', ... }` after the `/developers` route.
- `client/src/views/DeveloperDetailView.vue` — L0 page: breadcrumb, developer header (avatar + name + role + subTeam), sprint range selector (Last 5 / Last 10 / All). Child component placeholder (replaced in Steps 6-8).

**Build:** Passed.

---

### Step 6: Sprint Trends stacked bar chart component
**Status:** Done

**Files Created:**
- `client/src/components/developer-detail/SprintTrendsChart.vue` — L3 props-only component. Mixed stacked bar + line ApexCharts. Four series: Feature SP (blue bar), Bug SP (red bar), Completion % (gray line, secondary Y-axis 0-100%), Rolling Avg (amber dashed line). Dual Y-axes. Custom tooltip with 6 fields (sprint name, feature SP, bug SP, total SP, completion%, capacity%). Handles null `rollingAverageSp`.

**Build:** Passed.

---

### Step 7: Work Allocation chart and Current Sprint section
**Status:** Done

**Files Created:**
- `client/src/components/developer-detail/WorkAllocationChart.vue` — L3 props-only. Line chart for bug% per sprint. Dashed target reference line via `annotations.yaxis`. Marker colors red when above target, amber otherwise. Summary line below: average bug% and sprints above target count.
- `client/src/components/developer-detail/CurrentSprintSection.vue` — L1 feature component. Shows "No active sprint" when null. Full-width burnup chart (height 300). SP summary with "(all types)" label and feature/bug split. Renders `TicketTable` child.

**Build:** Passed.

---

### Step 8: Ticket table component
**Status:** Done

**Files Created:**
- `client/src/components/developer-detail/TicketTable.vue` — L3 props-only. Grouped by state (stalled/in-progress/not-started/done). Color-coded state group headers. Jira links open in new tab. Stalled tickets get left red border (`border-l-2 border-l-status-danger`). Null SP shows "—". Horizontal scroll wrapper for narrow viewports.

**Build:** Passed (after wiring into DeveloperDetailView — see below).

---

### Steps 6-8 wiring
**Status:** Done

**Files Modified:**
- `client/src/views/DeveloperDetailView.vue` — Replaced placeholder div with `SprintTrendsChart`, `WorkAllocationChart`, and `CurrentSprintSection` inside a `space-y-8` container.

**Pre-existing unused-import errors fixed (not introduced by this feature):**
- `client/src/components/developers/DeveloperProgressCard.vue` — Removed unused `BaseCard` import (template uses plain `<div>`).
- `client/src/components/developers/QaWorkloadMetricCards.vue` — Removed unused `deltaClass` from `useDeltaDisplay()` destructuring.
- `client/src/components/settings/UsersTab.vue` — Removed unused `authStore` inject and `authStoreKey` import.

**Build:** Passed (exit 0, 278 modules transformed).

---

### Step 9: Navigation links from DeveloperProgressCard
**Status:** Done

**Files Modified:**
- `client/src/components/developers/DeveloperProgressCard.vue` — Wrapped avatar (`<img>` and fallback `<div>`) in `<RouterLink>` to `developer-detail` route. Wrapped developer name in separate `<RouterLink>` with `hover:text-accent-default hover:underline`. Avatar gets `hover:ring-2 hover:ring-accent-default transition-shadow` for visual feedback. Breadcrumb was already implemented in DeveloperDetailView.vue in Step 5.

**Build:** Passed.

---

### Step 10: Analytics Targets subsection in Sync settings tab
**Status:** Done

**Files Modified:**
- `client/src/types/index.ts` — Added `bugRatioTarget: number` to `AppSettings` interface (after `defaultSpPerBug`).
- `client/src/api/settings.ts` — Added `saveAnalyticsTargets(bugRatioTarget)` function calling `PUT /settings/analytics-targets`.
- `client/src/stores/settingsStore.ts` — Added `bugRatioTarget: 30` to default state, added `saveAnalyticsTargetsAction`, exposed in return object.
- `client/src/views/SettingsView.vue` — Added `bugRatioTarget: 30` to form reactive init, added `bugRatioTarget: s.bugRatioTarget` to `syncFromStore()`.
- `client/src/components/settings/SyncTab.vue` — Added "Analytics Targets" `<section>` before the "Sync from Jira" section. Includes info description, "Bug ratio target (%)" number input (0-100), save button with saving/saved/error state following `saveSyncConfigPanel` pattern. Read-only guard on input and button.

**Build:** Passed (exit 0).

---

## Review Fixes (Cycle 1)

### [MEDIUM] Migration default value 0m → 30m
**File:** `src/Services/Fokus/Fokus.Persistence/Migrations/20260524082152_AddBugRatioTarget.cs`
Changed `defaultValue: 0m` to `defaultValue: 30m` so existing AppSettings rows get the intended default rather than 0 (which would make the work allocation chart reference line invisible at 0%).

### [HIGH] TakeLast applied after exclusion guard, not before
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperDetailService.cs`
Restructured sprint trends loop to run over all `qualifyingSprints` (full chronological list), apply exclusion guard (capacity==0 AND totalSp==0) inline, collect into `allSprintTrends`, then apply `TakeLast(last)` at the end. `last=5` now correctly returns exactly 5 qualifying sprints. Rolling average now receives `qualifyingSprints` (full list) so the 3-sprint look-back window is stable regardless of `last` cutoff.

### [LOW] TicketTable stall border moved from `<tr>` to first `<td>`
**File:** `client/src/components/developer-detail/TicketTable.vue`
Moved `border-l-2 border-l-status-danger` from `<tr>` to the Key `<td>`. Non-stalled rows get `border-l-2 border-l-transparent` for column alignment. Renders correctly regardless of `border-collapse` behavior.

**Builds after fixes:** Backend (Fokus.API) exit 0, frontend exit 0.

## Carry-Over

- The `dotnet build -q` flag on Fokus.API consistently triggers a spurious MSB3492 stale-cache error. Full build (without `-q`) succeeds. Build Persistence first then API with full output is the reliable verification path.
- The three pre-existing unused-import TypeScript errors (`BaseCard` in DeveloperProgressCard, `deltaClass` in QaWorkloadMetricCards, `authStore` in UsersTab) were blocking the frontend build. Fixed as part of Steps 6-8 wiring — not introduced by this feature.

## Deviations

| Step | Deviation | Reason |
|------|-----------|--------|
| Step 2 | Returns 200 `{success: true}` instead of spec's 204 | Every existing settings save endpoint returns 200 with body; frontend store expects this pattern |
| Step 4 | Explicit `using Jira.RestApi; using Microsoft.Extensions.Options;` | JiraOptions not in GlobalUsings; required explicit using same as SyncXrayEndpoint.cs |
