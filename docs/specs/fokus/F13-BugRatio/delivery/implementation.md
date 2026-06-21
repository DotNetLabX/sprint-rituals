# Bug Ratio (F13) — Implementation

## Files Created
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — All response records + BugRatioService with ComputeMultiSprint, ComputeSingleSprint, EvaluateAlert, and private helpers
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioQuery.cs` — GetBugRatioRequest + GetBugRatioRequestValidator
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — FastEndpoints GET endpoint, orchestrates sprint/developer loading and delegates to BugRatioService
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260509101214_AddBugRatioAlertSettings.cs` — EF migration adding BugRatioAlertThreshold and BugRatioConsecutiveSprintCount columns
- `client/src/components/developers/BugRatioTab.vue` — L1 container component, switches between multi and single sprint sub-components
- `client/src/components/developers/BugRatioMetricCards.vue` — L3 metric cards for team-level bug ratio summary
- `client/src/components/developers/BugRatioTrendChart.vue` — L3 ApexCharts line chart for bug ratio % trend (multi-sprint only)
- `client/src/components/developers/BugRatioStackedChart.vue` — L3 ApexCharts stacked bar chart for Bug SP vs Non-Bug SP per sprint (multi-sprint only)
- `client/src/components/developers/BugRatioDevTable.vue` — L3 per-developer table with alert badge and delta support
- `client/src/components/developers/BugRatioIssueTypeBreakdown.vue` — L3 completed ticket counts by Jira issue type

## Files Modified
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added BugRatioAlertThreshold (default 50) and BugRatioConsecutiveSprintCount (default 2); updated CreateDefault()
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Added two new fields to GetSettingsResponse
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Added two new fields to mapping block
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — Added two new fields to SaveSettingsCommand with defaults; added validation rules (0-100 and 1-10)
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — Added two new fields to AppSettings construction
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered BugRatioService as scoped
- `client/src/types/index.ts` — Added bugRatioAlertThreshold and bugRatioConsecutiveSprintCount to AppSettings; added all 14 BugRatio type interfaces
- `client/src/api/analytics.ts` — Added getBugRatio() function and BugRatioResponse import
- `client/src/stores/developersStore.ts` — Added activeTab, bugRatio, bugRatioLoading, bugRatioError state; added switchTab() and fetchBugRatio() actions; modified selectSprint/selectLastN/selectSubTeam to re-fetch bug ratio when tab is active
- `client/src/stores/settingsStore.ts` — Added bugRatioAlertThreshold and bugRatioConsecutiveSprintCount to default settings value
- `client/src/views/DevelopersView.vue` — Added tab bar UI, tab URL sync, BugRatioTab rendering; throughput content wrapped in tab conditional
- `client/src/views/SettingsView.vue` — Added form fields and syncFromStore mapping for new settings; added "Bug Ratio Alerts" section with two number inputs

## Key Decisions
- AppSettingsRepository.SaveAsync uses `SetValues` for scalar int fields — no manual field copies needed (unlike list properties with converters)
- EvaluateAlert is a non-static instance method (matches plan spec) but has no instance state; consistent with service class pattern
- BugRatioStackedChart aggregates SP across all developers per sprint at the team level (Bug SP total vs Non-Bug SP total per sprint), making it team-wide rather than per-developer stacked — this gives a clear team picture without a cluttered multi-developer stacking

## Deviations from Plan
- None. All 10 steps implemented as specified.

## Review Cycle 1 Fixes

### HIGH: Migration default values corrected
`src/Services/Fokus/Fokus.Persistence/Migrations/20260509101214_AddBugRatioAlertSettings.cs` — Changed `defaultValue: 0` to `defaultValue: 50` for `BugRatioAlertThreshold` and `defaultValue: 2` for `BugRatioConsecutiveSprintCount`. EF auto-generates 0 for int columns; the domain property defaults do not flow into the migration.

### HIGH: switchTab always refetches bug ratio data
`client/src/stores/developersStore.ts` — Removed `bugRatio.value === null` guard from `switchTab`. Now always fetches when switching to the Bug Ratio tab, ensuring data reflects current sprint/sub-team selection even when filters changed while on the Throughput tab.

### MEDIUM: Alert tooltip shows actual threshold value
`src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — Added `ThresholdPercent: int` to `BugRatioAlertStatus` record; `EvaluateAlert` passes `threshold` as third argument.
`client/src/types/index.ts` — Added `thresholdPercent: number` to `BugRatioAlertStatus` interface.
`client/src/components/developers/BugRatioDevTable.vue` — Tooltip now renders `"Bug ratio above ${dev.alert.thresholdPercent}% for ${dev.alert.consecutiveSprintCount} consecutive sprints"`.
