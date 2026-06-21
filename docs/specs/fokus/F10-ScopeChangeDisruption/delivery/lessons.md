# Scope Change & Disruption — Lessons

## Developer Lessons

- AppSettingsRepository.SaveAsync explicit list assignments: Any new List<string> property on AppSettings persisted via JSON converter must be explicitly reassigned in SaveAsync (alongside the SetValues call), because SetValues does not copy reference-type properties tracked via custom converters. This is now established pattern for all three list properties (DoneStatuses, WorkflowStages, ExcludedFromScopeStatuses).

- EF migration path: Must be run from the repo root (`dotnet ef migrations add ... -p src/Services/... -s src/Services/...`). The plan shows the path without the `src/` prefix — in this repo all source is under `src/`. Running from the working directory requires the full path.

- Burnup chart completion tracking: The burnup CompletedSp line requires status transitions for ALL non-removed tickets in the sprint, not just mid-sprint bugs. Loading transitions only for bugs leaves committed and non-bug added tickets with no transition data, so their completions never register on any day — the burnup line stays flat. Fix: load transitions for all non-removed ticket IDs. ComputeBugTimeInProgress filters internally to mid-sprint bugs, so the same transition list safely serves both purposes. When writing an endpoint that loads transitions for a subset and passes them to a service, verify every consumer of that list — if any consumer needs a broader set, widen the query at the endpoint.

- GetActiveStatuses heuristic: WorkflowStages convention is [backlog-ish, ...active statuses..., done-ish]. When count is 1 or 2, all stages are treated as active. When count > 2, middle stages are active. This is a pragmatic heuristic — if requirements change (e.g., first stage is active), update the helper.

- TypeScript union type for store default: sprintsStore.selectedLast defaults to `5` (not null) and sprintMode defaults to `'multi'`, unlike developersStore where selectedLast starts null and mode starts 'single'. The initialize() function does not override these if already set from URL params — this is correct because onMounted sets store state from URL before calling initialize().

## Reviewer Lessons

- **EF migration `defaultValue` for JSON columns:** When EF Core scaffolds an `AddColumn` migration for a `NOT NULL` column that uses a JSON converter, it generates `defaultValue: ""`. This is wrong for JSON columns — existing rows get an empty string, which `JsonSerializer.Deserialize` throws on (not null, so the `?? fallback` doesn't help). Always manually set `defaultValue: "[]"` (or the appropriate JSON default) for JSON-serialized list columns added via migration. Check every `AddColumn` in a new migration against its converter.

- **"All" mode needs a backend sentinel:** When a store has a null-means-all pattern for a list size parameter, the backend must explicitly handle null/absent param as "all" — it cannot just default to a reasonable N. The null-coalescing `?? 5` pattern only works when there's no "all" option. If "all" is in scope, coordinate frontend sentinel (e.g., `last=0`) with backend branching before coding.

## Architect Lessons

- **Burnup data dependencies are broader than they appear.** The plan specified loading status transitions only for mid-sprint bug tickets (Step 6, single-sprint mode, item 9b). The Step 1 done check caught that the burnup chart's CompletedSp line also needs transitions for ALL non-removed tickets to plot daily completion. When planning data-loading steps for an endpoint that feeds multiple computations, trace every consumer of the loaded data -- not just the one named in the step title. The developer's key decision to widen the query was correct and necessary.

- **Migration defaults for JSON columns need explicit plan callout.** The plan specified the property default (`[]`) and the JSON converter pattern but did not explicitly call out that the EF migration's `defaultValue` must be manually corrected from `""` to `"[]"`. This is a known EF Core gotcha (already in `ef-core-gotchas.md` conceptually, but not specific to migration scaffolding). Future plans adding JSON-converted columns should include a migration verification step: "After scaffolding the migration, verify the `defaultValue` in the generated migration file is valid JSON (e.g., `"[]"`), not an empty string."

- **"All items" sentinel must be planned end-to-end.** The plan specified `last=null` means "all" but did not trace the full chain: store state (null) -> API function parameter (undefined when null) -> query string (absent) -> endpoint fallback (`?? 5` = last 5, not all). The reviewer caught this as a HIGH finding. When a UI option maps to "no limit," the plan should specify the sentinel value and trace it through every layer: store -> API function -> query string -> validator -> endpoint branching.

- **Plan step 6 response wrapper was well-specified.** The discriminated wrapper pattern (mode + nullable MultiSprint + nullable SingleSprint) worked cleanly. No ambiguity for the developer. This pattern should be reused for future multi-mode analytics endpoints.

## Skill Gaps

- **Missing skill:** `create-vue-page` — a skill covering the full pattern of creating a new page view: store creation, URL sync watcher, toolbar wiring, conditional mode rendering, component extraction. Steps 9 and 10 both needed this. Reference files: `client/src/views/DevelopersView.vue`, `client/src/stores/developersStore.ts`.

- **Missing skill:** `create-apex-chart` — patterns for ApexCharts integration in Vue (series shapes, options structure, dark theme tokens, event handling via chart.events). Referenced `DevelopersView.vue` chart patterns and extended them. Suggested coverage: bar/line/area chart configs, color tokens, dark theme setup, click events.
