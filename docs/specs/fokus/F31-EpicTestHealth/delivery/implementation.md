# Epic Test Health — Implementation

## Files Created

None — all changes are additive to existing files.

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — Added `GetTestExecutionDataForTicketsAsync` method: bulk-loads all non-cancelled TE links (Tests and Blocks) and test runs for a given set of ticket keys in 2 queries (links + runs), returning three grouped lookups used by the service.

- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — Extended response record hierarchy (`EpicProgressTicketEntry` gains 4 QA fields, `EpicProgressEntry` gains 7 QA fields, `EpicProgressResponse` gains `HasQaData` + `AverageTestCoverage`); added `EpicQaData` parameter record; extended `ComputeEpicProgress` signature with optional `qaData`; added private helpers `ComputeEpicQaMetrics` (per-epic BR1–BR4, BR12, BR13, BR20) and `BuildTicketEntry` (per-ticket BR5–BR8); added average test coverage computation (BR16) in the return path. **Fix cycle 1:** removed dead-code `blocksTeIds` variable and empty `foreach` loop from `ComputeEpicQaMetrics` (leftover from mid-implementation refactor of BR4 logic); fixed all-aborted test runs falling through to `"Passed"` — final `else` branch in `BuildTicketEntry` now returns `"NoTests"` (TEs exist but all runs are aborted = no effective test results, per BR5 which defines Passed as all-PASS).

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs` — Injected `TestExecutionRepository`; added step 9 that conditionally loads QA data when `XrayEnabled` (no DB round-trip when disabled, per plan step 3 pattern from `GetQaMetricsEndpoint`); updated early-return empty response to include new required fields; passes `qaData` to service.

- `client/src/types/index.ts` — Extended `EpicProgressTicketEntry` with 4 QA fields (`testStatus`, `testPassRate`, `testBugsFound`, `testRunSummary`); extended `EpicProgressEntry` with 7 QA fields (`coverageRate`, `passRate`, `bugsFound`, `featureTicketCount`, `coveredTicketCount`, `coverageRag`, `passRateRag`); extended `EpicProgressResponse` with `hasQaData` and `averageTestCoverage`.

- `client/src/stores/epicsStore.ts` — Imported and composed `useSettingsStore`; added `isXrayEnabled` computed (reads `settings.value.xrayEnabled` via `storeToRefs`); added `averageTestCoverage` computed (reads from `epicProgress.value?.averageTestCoverage`); updated `initialize()` to call `settingsStore.fetchSettings()` in the `Promise.all`; exported both new computeds. **Fix cycle 1:** switched from direct `settingsStore.settings.xrayEnabled` to `storeToRefs(settingsStore)` destructure per `pinia-patterns` convention.

- `client/src/components/epics/EpicSummaryCards.vue` — Added `hasQaData` and `averageTestCoverage` props; grid changes to `lg:grid-cols-4` when QA card is visible; added 4th "Average Test Coverage" `BaseCard` conditionally rendered when `hasQaData && averageTestCoverage !== null`, shows `XX.X%` and a `bg-emerald-500` progress bar.

- `client/src/components/epics/EpicTable.vue` — Added `hasQaData` prop; added `ragClass` helper (matches `QualityDevTable.vue` pattern); added 3 conditional QA header columns (Coverage %, Pass Rate %, Bugs Found); updated progress bar cell to stack SP bar and a thinner `h-1` coverage bar (`bg-emerald-500`, conditionally shown); added QA data cells with RAG coloring for coverage/pass rate; updated expanded row `colspan` from 8 to 11 when `hasQaData`; passes `hasQaData` down to `EpicTicketTable`.

- `client/src/components/epics/EpicTicketTable.vue` — Added `hasQaData` prop; added `testStatusClass` and `testStatusLabel` helpers; added 3 conditional QA header columns (Test Status, Pass Rate, Bugs Found); added corresponding data cells — Test Status shows colored badge matching `bg-status-{color}/10 text-status-{color}` pattern, Pass Rate shows percentage, Bugs Found shows count; bug tickets (`testStatus === null`) show "—" in all QA cells.

- `client/src/views/EpicsView.vue` — Passes `has-qa-data` and `average-test-coverage` to `EpicSummaryCards`; passes `has-qa-data` to `EpicTable`.

- `docs/kb/analytics/epic-progress.md` — Added QA Metrics section documenting computation formulas, key business rules, and file references.

## Key Decisions

- **`EpicQaData` as a named record parameter** rather than 3 separate tuple parameters: cleaner call site, easier to extend later.
- **`qaData = null` as the Xray-disabled sentinel**: the service's `null` check gates all QA computation paths cleanly without adding a boolean flag parameter.
- **`BlocksLinksByTicket` inversion for bugs found**: The repository stores `bugKey → [teIds]`. To find bugs for an epic, the service collects all TE IDs used by the epic's feature tickets, then filters `BlocksLinksByTicket` entries whose TE IDs intersect. This avoids a second pass over all tickets.
- **`initialize()` fetches settings**: Required so `isXrayEnabled` computed is accurate before QA columns render. Settings are cached by the store, so repeat calls are idempotent.
- **`averageTestCoverage` as a store computed** (not view-local): The view needs it for the summary card; computing it in the store keeps the view lean and consistent with the store pattern.
- **Coverage bar uses `h-1` vs SP bar `h-2`**: Thinner bar makes the dual-bar layout visually distinct without adding a legend.

## Deviations from Plan

- **`initialize()` Promise.all returns `[teams]` destructuring only**: `Promise.all` now includes 3 promises (`getSubTeams`, `settingsStore.fetchSettings`, `fetchEpicProgress`). The destructuring pattern `const [teams] =` was kept — `fetchEpicProgress` and `settingsStore.fetchSettings` mutate store state directly (no return value needed). This matches the existing pattern in the store.
- **`Sprints` column gets `pr-4` class when `hasQaData`**: The plan says "update colspan" but didn't specify the column spacing adjustment needed when QA columns are appended. Added `pr-4` to the last non-QA column when `hasQaData` to prevent cells touching.
