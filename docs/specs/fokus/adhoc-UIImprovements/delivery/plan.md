# UI Improvements (Burnup Tooltip, Team Table, Developer Sorting, Settings Tabs)

**Feature Spec:** None — scoped UI improvements with requirements captured in conversation + screenshots.

## Context

Four independent UI polish items requested together. No domain model changes. One backend change (burnup ticket counts) and one backend restructuring (settings endpoint split). The rest is frontend-only.

## Scope

**In scope:**
1. Burnup chart: show ticket counts in tooltip brackets, change x-axis to day-of-week format
2. Team table: each sub-team table sizes independently
3. Developers: sortable columns on both Throughput and Bug Ratio tables
4. Settings: tabbed layout with per-section save endpoints

**Out of scope:**
- Settings page component extraction (keep single file with tabs, extract later if needed)
- Backend changes to Throughput or Bug Ratio endpoints

## Domain Model Changes

None.

## Data Model Changes

- `BurnupDataPoint` record gains 3 new fields: `int TotalScopeTickets`, `int CompletedTickets`, `int BugTickets`. No migration — computed in-memory, not persisted.

## Implementation Steps

### Group 1: Burnup Chart

#### Step 1 — Add ticket counts to BurnupDataPoint (backend)

**What:** Extend the `BurnupDataPoint` record with cumulative ticket counts and compute them in `BuildBurnupData`.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`
  - Add `int TotalScopeTickets, int CompletedTickets, int BugTickets` to the `BurnupDataPoint` record (line 54–60)
  - In `BuildBurnupData` (line 393–497): add 3 counters alongside the SP accumulators. Use the same LINQ filters but `.Count()` instead of `.Sum()`:
    - `startingTotalScopeTickets` = count of committed, not-removed, not-excluded memberships
    - `startingBugTickets` = same filtered to `IssueType == "Bug"`
    - Per-day: count additions, removals, completions (same day filters as SP)
    - `cumulativeCompletedTickets` += tickets with done transition on this day
    - `cumulativeBugTickets` += bug added - bug removed - bug completed (mirrors bug SP logic)
  - Update the `result.Add(new BurnupDataPoint(...))` call to include all 3 counts

**Pattern:** Follow the existing `cumulativeBugSp` accumulation pattern — same filters, `.Count()` instead of `.Sum()`.

**Depends on:** Nothing.

#### Step 2 — Update BurnupChart tooltip and x-axis (frontend)

**What:** Show ticket counts in brackets in the tooltip. Change x-axis labels from `"Day N (DD Mon)"` to `"Day N (Mon)"`.

**Files to modify:**
- `client/src/types/index.ts` — Add `totalScopeTickets: number`, `completedTickets: number`, `bugTickets: number` to `BurnupDataPoint` interface (line 330–335)
- `client/src/components/sprints/BurnupChart.vue`:
  - `xLabels` computed (line 13–17): change `toLocaleDateString('en-GB', { month: 'short', day: 'numeric' })` to `toLocaleDateString('en-US', { weekday: 'short' })` → produces `"Day 14 (Mon)"`
  - `chartOptions.tooltip` (line 70): replace `{ theme: 'dark' }` with a custom formatter that shows ticket counts in brackets:
    ```
    Bug SP: 76 [12]
    Total Scope SP: 241 [38]
    Completed SP: 95 [15]
    ```
    Use `tooltip.custom` or `tooltip.y.formatter` with access to `dataPointIndex` to look up `props.burnupData[dataPointIndex].bugTickets` etc.

**Depends on:** Step 1.

### Group 2: Team Table

#### Step 3 — Team table independent sizing (frontend)

**What:** Each sub-team table should auto-size to its own content instead of stretching full-width.

**Files to modify:**
- `client/src/components/team/TeamTable.vue` (line 50):
  - Change `<table class="w-full text-sm">` to `<table class="w-auto text-sm">`
  - On the wrapper `<div class="overflow-x-auto ...">`, keep `overflow-x-auto` for narrow screens but remove any implicit full-width stretch — the table will size to content

**Depends on:** Nothing.

### Group 3: Developer Sorting

#### Step 4 — Add column sorting to Throughput tables (frontend)

**What:** Add clickable sort headers that toggle ascending/descending on every numeric column in both single-sprint and multi-sprint throughput tables.

**Files to modify:**
- `client/src/views/DevelopersView.vue`:
  - Add sort state: `sortColumn: string | null`, `sortDirection: 'asc' | 'desc'`
  - Add `sortedSingleSprintRows` computed that sorts `singleSprintRows` by the active column/direction
  - Add `sortedMultiSprintDevelopers` computed that sorts `store.throughput.developers`
  - Replace `<th>` elements with clickable headers that call a `toggleSort(column)` function
  - Add visual indicator (arrow) showing current sort column and direction
  - Sortable columns — single: SP Assigned, SP Completed, Completion %, Tickets Done, Carried Over, Capacity %; multi: same set using avg functions
  - Use `v-for="row in sortedSingleSprintRows"` instead of `singleSprintRows`; same for multi

**Pattern:** Local component sort state (no store). Sort function extracts the value based on column key, handles numeric comparison. Clicking same column toggles direction; clicking different column sets ascending.

**Depends on:** Nothing.

#### Step 5 — Add column sorting to Bug Ratio tables (frontend)

**What:** Same sorting behavior for both single-sprint and multi-sprint Bug Ratio tables.

**Files to modify:**
- `client/src/components/developers/BugRatioDevTable.vue`:
  - Add sort state: `sortColumn`, `sortDirection` (same pattern as step 4)
  - Add `sortedMultiDevelopers` and `sortedSingleDevelopers` computeds
  - Replace `<th>` elements with clickable headers
  - Sortable columns: Bug SP, Non-Bug SP, Bug Ratio %, Bug Tickets, Non-Bug Tickets
  - Use sorted arrays in `v-for`

**Pattern:** Same as step 4.

**Depends on:** Nothing.

### Group 4: Settings Tabs + Per-Section Save

#### Step 6 — Create per-section settings save endpoints (backend)

**What:** Create 6 new endpoints, each saving only its section of AppSettings. All follow the same pattern as `SaveExcludedStatusesEndpoint`: load existing → update fields → save → return.

**Files to create (all under `src/Services/Fokus/Fokus.API/Features/Settings/`):**

1. `SaveBoard/SaveBoardEndpoint.cs` — `[HttpPut("/api/settings/board")]`
   - Request: `{ int? BoardId }`
   - Updates `existing.BoardId`, saves, returns `{ success: true }`

2. `SaveDoneStatuses/SaveDoneStatusesEndpoint.cs` — `[HttpPut("/api/settings/done-statuses")]`
   - Request: `{ List<string> Statuses }`
   - Updates `existing.DoneStatuses`, saves, returns the updated list

3. `SaveWorkflowStages/SaveWorkflowStagesEndpoint.cs` — `[HttpPut("/api/settings/workflow-stages")]`
   - Request: `{ List<string> Stages }`
   - Updates `existing.WorkflowStages`, saves, returns the updated list

4. `SaveHealthConfig/SaveHealthConfigEndpoint.cs` — `[HttpPut("/api/settings/health-config")]`
   - Request: `{ HealthThresholdConfig HealthThresholds, HealthWeightConfig HealthWeights }`
   - Validates `weights sum == 100`
   - Updates both `existing.HealthThresholds` and `existing.HealthWeights`, saves, returns `{ success: true }`

5. `SaveBugRatioAlerts/SaveBugRatioAlertsEndpoint.cs` — `[HttpPut("/api/settings/bug-ratio-alerts")]`
   - Request: `{ int AlertThreshold, int ConsecutiveSprintCount, int DefaultSpPerBug }`
   - Updates the 3 fields, saves, returns `{ success: true }`

6. `SaveSyncConfig/SaveSyncConfigEndpoint.cs` — `[HttpPut("/api/settings/sync-config")]`
   - Request: `{ int SyncBackSprintCount, int PlanningWindowDays }`
   - Updates both fields, saves, returns `{ success: true }`

**Pattern:** Follow `SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs` (line 18–33). All endpoints require `[Authorize(Roles = "Admin")]`.

**Depends on:** Nothing.

#### Step 7 — Add frontend API functions + update store (frontend)

**What:** Add API functions for the 6 new endpoints and update settingsStore to use per-section saves instead of the blob.

**Files to modify:**
- `client/src/api/settings.ts` — Add 6 new functions: `saveBoard`, `saveDoneStatuses`, `saveWorkflowStages`, `saveHealthConfig`, `saveBugRatioAlerts`, `saveSyncConfig`. Remove `saveSettings`.
- `client/src/stores/settingsStore.ts` — Remove `updateSettings`. Add per-section save actions that call the new API functions and update `settings.value` partially. Keep `fetchSettings` unchanged (still loads full settings).

**Depends on:** Step 6.

#### Step 8 — Refactor SettingsView into tabbed layout with per-panel save (frontend)

**What:** Add tab navigation to the settings page. Move the big "Save Settings" button into per-panel save buttons. Organize existing sections into tabs.

**File to modify:** `client/src/views/SettingsView.vue`

**Tab structure:**

| Tab | Sections | Save behavior |
|-----|----------|---------------|
| **Jira** | Jira Board, Done Statuses, Excluded Statuses | Board: save on change or save button. Done Statuses: own save button. Excluded: already has own save button. |
| **Workflow** | Workflow Stages, Cycle Time Boundaries | Workflow Stages: own save button. Boundaries: already has own save button. |
| **Health** | Health Thresholds + Health Weights (one panel), Bug Ratio Alerts | Health Config: one save button (validates weights sum). Bug Ratio: own save button. |
| **Sync** | Sync from Jira (sync config + sync controls) | Sync Config: own save button. Sync All/Range: existing buttons. |
| **Users** | User Management (Admin only) | Already has inline saves (role/status changes fire immediately). |

**Changes:**
- Add `activeSettingsTab` ref, default `'jira'`
- Add tab bar (same pattern as DevelopersView tab bar, line 229–252)
- Wrap each group of sections in `v-if="activeSettingsTab === 'xxx'"` 
- Remove the single "Save Settings" button at the bottom (line 931–941)
- Add per-panel save buttons that call the new store actions:
  - "Save Board" → `store.saveBoard(form.boardId)`
  - "Save Done Statuses" → `store.saveDoneStatuses(form.doneStatuses)`
  - "Save Workflow Stages" → `store.saveWorkflowStages(form.workflowStages)`
  - "Save Health Config" → `store.saveHealthConfig(form.healthThresholds, form.healthWeights)` (disabled if weights sum !== 100)
  - "Save Bug Ratio Alerts" → `store.saveBugRatioAlerts(...)`
  - "Save Sync Config" → `store.saveSyncConfig(form.syncBackSprintCount, form.planningWindowDays)`
- Update `syncAll` to call `saveSyncConfig` instead of `save()`
- Remove the old `save()` function
- Each save button: same pattern as existing `saveExcluded` — saving/saved/error refs, 3-second success flash

**Depends on:** Step 7.

#### Step 9 — Retire blob SaveSettingsEndpoint (backend)

**What:** Delete the old blob endpoint now that all sections have dedicated endpoints.

**Files to delete:**
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/` directory (including SaveSettingsCommand, SaveSettingsResponse if they're sibling files)

**Files to verify:** Grep for `SaveSettingsCommand` and `SaveSettingsResponse` across the solution to confirm no other consumers.

**Depends on:** Steps 7, 8.

## Cross-Service Changes

None — single service.

## Migration Notes

No database migration. `BurnupDataPoint` changes are purely in the computation layer. Settings endpoints update the same `AppSettings` entity through the existing repository.

## Testing Strategy

- **Burnup tooltip:** Load a sprint with known tickets → verify tooltip shows SP values with ticket counts in brackets. Verify x-axis shows day names (Mon, Tue, etc.) instead of dates.
- **Team table:** Verify each sub-team group table sizes to its content, not full-width.
- **Developer sorting:** Click each column header → verify rows reorder. Click again → verify direction toggles. Test both throughput and bug ratio tables in both single and multi sprint modes.
- **Settings tabs:** Navigate each tab → verify correct panels shown. Use each per-panel save → verify success flash and persistence. Verify Sync All still works (saves sync config before syncing). Verify old blob endpoint is gone (404 on PUT /api/settings).

## Open Questions

None.
