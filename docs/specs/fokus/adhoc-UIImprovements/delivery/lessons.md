# UI Improvements — Lessons

## Developer Lessons

- When the app process is running and holding DLL file locks, `dotnet build` emits MSB3027 file lock errors but zero `error CS` compile errors. Filtering for `error CS` in build output is the right way to verify compile-only correctness without needing to stop the running process.
- ApexCharts `tooltip.custom` receives `{ seriesIndex, dataPointIndex, w }` — access `w.globals.categoryLabels[dataPointIndex]` for the x-axis label. The `props.burnupData` array can be accessed directly by index since it is 1:1 with chart data points.
- Vue `<template v-if>` is preferable to `<div v-if>` for tab wrappers in settings pages — it avoids adding DOM wrapper elements that could affect spacing or layout.
- When removing a shared "save all" function used by multiple callers (like syncAll), audit every call site before deletion to avoid runtime errors. The syncAll function called save() internally and needed its own targeted save calls.
- PowerShell `Remove-Item -Recurse -Force` is required to delete directories on Windows; bash `rm -rf` is not available in the Bash tool on this environment.

## Architect Lessons

- Multi-group plans (4 independent groups, 9 steps) with clear "depends on" annotations work well for parallel implementation — developer completed all groups without blocking questions.
- Specifying "same pattern as step N" for repeated patterns (Steps 4 and 5 both use local sort state) is effective — developer applied it consistently without drift.
- Explicit "files to delete" + "grep to confirm no consumers" instructions in cleanup steps (Step 9) prevent orphaned references — verified zero remaining references.
- For plans without a feature spec (UI polish bundles), capturing scope explicitly as a numbered list in the plan's Scope section is sufficient for done-check traceability.

## Reviewer Lessons

- When `dotnet build` fails with MSB3027 file-lock errors, run with `-o <separate-output-dir>` to avoid the lock and then filter output for `error CS` to verify compile correctness independently of the running process.
- The `store.error` shared-ref pattern (single error ref for all store actions) creates a fragile error-detection path in views that call `if (!store.error)` after `await store.actionX()`. Flag as MEDIUM: it works for sequential single-user saves but breaks under concurrency. The fix is to have actions `throw` so the view's own `try/catch` handles it.
- When reviewing partial-save refactors (blob endpoint split into N focused endpoints), verify: (1) all field groups are covered, (2) the old endpoint and its siblings are fully deleted, (3) no remaining consumers via grep. All three checks should be explicit in the review evidence table.
- ApexCharts `tooltip.custom` supersedes `tooltip.theme` — having both is dead code. File as LOW.

## Skill Gaps

- **Missing skill:** "add-sortable-table" — covers adding local sort state (sortColumn/sortDirection refs), toggle function, sort icon helper, and sorted computed — used in Steps 4 and 5. Reference files: DevelopersView.vue, BugRatioDevTable.vue.
- **Missing skill:** "settings-tab-layout" — covers adding tab nav bar to a settings view with per-panel save buttons matching the saveExcluded pattern. Reference files: SettingsView.vue.
