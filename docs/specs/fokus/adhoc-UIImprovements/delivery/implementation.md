# UI Improvements — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBoard/SaveBoardEndpoint.cs` — PUT /api/settings/board; updates BoardId only, returns { success: true }
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveDoneStatuses/SaveDoneStatusesEndpoint.cs` — PUT /api/settings/done-statuses; updates DoneStatuses, returns updated list
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveWorkflowStages/SaveWorkflowStagesEndpoint.cs` — PUT /api/settings/workflow-stages; updates WorkflowStages, returns updated list
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigEndpoint.cs` — PUT /api/settings/health-config; updates HealthThresholds + HealthWeights, validates weights sum = 100
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsEndpoint.cs` — PUT /api/settings/bug-ratio-alerts; updates AlertThreshold, ConsecutiveSprintCount, DefaultSpPerBug
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSyncConfig/SaveSyncConfigEndpoint.cs` — PUT /api/settings/sync-config; updates SyncBackSprintCount and PlanningWindowDays

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added TotalScopeTickets, CompletedTickets, BugTickets to BurnupDataPoint record; added 3 ticket count accumulators in BuildBurnupData using same filters as SP accumulators but with .Count() instead of .Sum()
- `client/src/types/index.ts` — Added totalScopeTickets, completedTickets, bugTickets to BurnupDataPoint interface
- `client/src/components/sprints/BurnupChart.vue` — Changed x-axis format from "DD Mon" to weekday short name ("Mon"); replaced tooltip: { theme: 'dark' } with custom tooltip formatter showing SP values with ticket counts in brackets
- `client/src/components/team/TeamTable.vue` — Changed table class from w-full to w-auto so each sub-team table sizes to its content
- `client/src/views/DevelopersView.vue` — Added ref import; added throughputSortColumn/throughputSortDirection state; added toggleThroughputSort/throughputSortIcon helpers; added sortedSingleSprintRows and sortedMultiSprintDevelopers computeds; updated both throughput table th elements to be clickable with sort indicators; swapped v-for to use sorted arrays
- `client/src/components/developers/BugRatioDevTable.vue` — Changed defineProps to use props variable; added ref/computed imports; added bugRatioSortColumn/bugRatioSortDirection state; added toggle/icon/sort-value helpers; added sortedMultiDevelopers and sortedSingleDevelopers computeds; updated both table headers to be clickable; swapped v-for to use sorted arrays
- `client/src/api/settings.ts` — Removed saveSettings; added 6 new per-section save functions (saveBoard, saveDoneStatuses, saveWorkflowStages, saveHealthConfig, saveBugRatioAlerts, saveSyncConfig); added HealthThresholdConfig/HealthWeightConfig to imports
- `client/src/stores/settingsStore.ts` — Removed updateSettings action; added 6 per-section save actions (saveBoardAction etc.) each updating settings.value partially on success; updated imports and return object
- `client/src/views/SettingsView.vue` — Added activeSettingsTab ref defaulting to 'jira'; added tab bar (Jira/Workflow/Health/Sync/Users); added 6 per-panel save functions with saving/saved/error state matching saveExcluded pattern; removed old save() function; updated syncAll to call saveSyncConfigAction + saveBoardAction instead of save(); wrapped each section group in v-if tab; added per-panel save buttons; removed global "Save Settings" button; added Save Board, Save Done Statuses, Save Workflow Stages, Save Health Config, Save Bug Ratio Alerts, Save Sync Config buttons in their respective sections

## Files Deleted

- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — Retired blob endpoint; replaced by 6 per-section endpoints
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — No other consumers; deleted with endpoint

## Key Decisions

- BugTickets accumulator mirrors bug SP logic exactly: starts with committed bugs, adds bug additions, subtracts bug removals and bug completions per day — matching the cumulativeBugSp pattern precisely
- Tooltip custom formatter accesses props.burnupData directly by dataPointIndex for type safety instead of relying on ApexCharts series data
- Sort state is local to the component (not store) per plan spec
- Both single and multi sprint tables in DevelopersView share a single sort state (throughputSortColumn/Direction) so sorting persists across mode switches — consistent UX
- Both bug ratio tables share a single sort state in BugRatioDevTable for same reason
- SettingsView tab wrapping uses `<template v-if>` not `<div v-if>` to avoid extra DOM wrapper elements changing layout
- Users tab button has `v-if="authStore.isAdmin"` matching the existing admin-only guard
- syncAll saves syncConfig and board (the two fields it actually uses) rather than all settings, avoiding side-effects on other sections

## Deviations from Plan

- None. All 9 steps implemented as specified.
