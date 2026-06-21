# F26-SprintTestCoverage — Lessons

## Architect Lessons
- The Ticket entity lacks a ParentTicketKey field even though the Jira DTO carries parent info. When a spec references sub-task relationships (like BR9 sub-task coverage inheritance), verify that the domain model actually persists the parent link before planning -- don't assume it exists because the DTO has it.
- The F25 plan explicitly noted that sprint-scoped TE queries (joining TestExecutionLink -> SprintMembership) were deferred to F26. This forward-reference pattern in plans is valuable -- it reduced F26 planning time because the expected method signature was already documented.
- Health score interpolation methods (ScoreHigherIsBetter, ScoreLowerIsBetter) are private statics in SprintSummaryService. When a new feature needs to reuse these, plan the extraction step explicitly. Shared computation utilities should be planned as a dedicated step, not buried inside another step.
- When extending an existing request model (like SaveHealthConfigRequest), explicitly state whether new fields follow the existing nullability model (non-nullable full-replace) or introduce a different model (nullable partial-update). Mixed models within the same request are confusing. Default to matching the existing pattern.
- [TRACKED] When a plan step modifies a frontend API function signature, trace the full call chain: API module -> store action -> view call site. All three must be listed explicitly. Missing the view call site was caught by the critic -- it's easy to forget the last hop.
- Repository query methods that determine "active scope" must explicitly list the filtering criteria rather than saying "same as X." The criteria (RemovedAt, IssueType, excluded statuses, IsStartedInSprint) are non-obvious and easily forgotten.
- "hasQaData" semantics need product decisions, not just technical ones. "Zero TEs after sync" and "qualityWeight=0" are edge cases that affect scoring and UX differently -- escalate these to the user early rather than defaulting to one interpretation.

## Developer Lessons

- [TRACKED] **TransitionAttributionChecker lives in Fokus.API, not Fokus.Persistence**: Repository methods needing active-scope filtering (IsStartedInSprint) cannot call the API-layer static helper. Replicate the simple transition predicate inline in the repository with private helpers (GetStageIndex, GetEffectiveSp). Acceptable duplication given the project reference graph constraint.
- **Non-positional `init` property on positional record**: C# records support `init` properties after positional parameters without breaking existing call sites. Syntax: add property with `{ get; init; }` in the record body. Used for `MetricCard.Rag` and `HealthScoreResult` Quality fields.
- **Extending ComputeSummary with optional quality parameters**: Added nullable optional parameters (`qualitySubScore = null`, `hasQaData = false`) to preserve all existing call sites without modification.
- **`dotnet build` accepts only one project per invocation**: Passing two `.csproj` paths fails with MSB1008. Build each project in a separate command.
- **EF Core two-step attribution tiebreaker (BR8)**: Finding the max SprintId per TE requires loading TE IDs linked to a sprint, then joining back to SprintMemberships grouped by TE ID to find Max(SprintId). EF Core translates this to SQL correctly.

## Architect Lessons (Step 1 Review)
- When a plan specifies a method signature for a repository that calls a helper from a different project layer (e.g., TransitionAttributionChecker in API referenced from Persistence), the developer will need to inline the logic. The plan should note this cross-layer constraint explicitly rather than referencing the helper by name. In F26, the developer handled this correctly by adding private helpers, but the plan should have anticipated it.
- The `defaultSpPerBug` parameter was missing from the planned `GetFeatureTicketsWithCoverageAsync` signature even though the method needs it for `GetEffectiveSp`. When planning complex repository methods, trace every dependency of the filtering logic to ensure the method signature is complete.
- The plan's hasQaData determination described a complex "synced state" detection mechanism, but the implementation correctly simplified to `XrayEnabled == true`. When product decisions make a simpler implementation valid, the plan should state the simplest viable approach rather than over-specifying.

## Developer Lessons (Phase 2)

- **MetricCard.vue tooltip wiring uses name-based lookup**: The existing pattern for metric tooltips is a `metricTooltip(metric.name)` function in the component, not a tooltip prop. New metric names (Coverage Rate, Execution Rate, Pass Rate) must be added to that function — not wired via slot or prop from a parent component.
- **SettingsView.vue form is a `reactive<AppSettings>`**: All new AppSettings fields must be initialised in the `form = reactive<AppSettings>(...)` object AND in `syncFromStore()`. Missing either location means the field is either never editable or never updated from the server.
- **InfoTooltip not imported in SettingsView.vue by default**: The component was added to the Settings template for the first time in F26. Always check whether a shared component is imported before using it in a new location.
- **saveHealthConfig full chain has three hops**: API function (`client/src/api/settings.ts`) → store action (`settingsStore.ts saveHealthConfigAction`) → view call site (`SettingsView.vue saveHealthConfigPanel`). All three must be updated together when the signature changes.
- **QA section v-if gates are on two conditions**: `xrayEnabled` (from settingsStore) determines whether the Quality section renders in Settings; `xrayEnabled && qaMetrics !== null` determines whether QaMetricsSection renders in Dashboard. The second gate means a failed API call silently hides the section without an error state — acceptable per spec.
- **Promise.all for parallel store initialisation**: When DashboardView needs settings before deciding whether to fetch QA data, fetch settings in parallel with `store.initialize()` via `Promise.all`, then branch on the result. Sequential fetches would add unnecessary latency.

## Architect Lessons (Phase 2 Step 1 Review)
- When a plan specifies TypeScript interface field names (e.g., `failedRunCount`, `totalRunCount` for FailingTicket), those names are binding contracts per plan writing rules. The plan should explicitly state that TypeScript field names must match the camelCase serialization of the backend C# property names. In F26, `FailingTicketItem.FailedRunCount` -> `failedRunCount`, but the TypeScript used `failCount`/`totalCount` -- a silent runtime mismatch.
- When a plan specifies response model shapes for expandable ticket lists that are consumed by display templates (not just stored), include ALL fields from the spec's acceptance criteria in the TypeScript interface definition. The plan said "list of `{ ticketKey, summary, assigneeName, storyPoints, failedRunCount, totalRunCount }`" but the TypeScript interface omitted `assigneeName` and `storyPoints`.
- Plans that say "Update tooltip text to include X" should specify the exact new tooltip text, not leave it to developer discretion. The HealthScoreBadge tooltip was left unchanged because the plan described what to do but not what the new text should be.

## Skill Gaps

- **Missing skill: extend-existing-service** — Pattern for extending a computation service (new parameters with defaults, extracting shared static helpers, extending records with init properties) without breaking existing call sites. Reference files: `SprintSummaryService.cs`, `HealthScoreCalculator.cs`.
- **Missing skill: repository-cross-entity-query** — Complex repository methods joining multiple entities for sprint-scoped attribution with tiebreaker logic (TestExecutionLink -> SprintMembership -> group by max SprintId). Reference files: `TestExecutionRepository.cs`.
- **Missing skill: vue-settings-panel-extension** — Pattern for adding a new settings sub-group to SettingsView: form reactive object init, syncFromStore update, save call site update, template section with v-if gate, InfoTooltip import. Reference files: `SettingsView.vue`, `settingsStore.ts`, `client/src/api/settings.ts`.

## Reviewer Lessons

- **Dead code in sparkline guards**: When a guard condition (e.g., `ContainsKey`) is evaluated after the protected variable is already obtained via `GetValueOrDefault`, and the caller always populates that key, the guard is unreachable. Look for this pattern in sparkline/window building loops where the caller's dictionary population precedes the service's guard.
- **Check all fields in parallel template sections for consistency**: When a component has two parallel expandable list sections (e.g., Untested Tickets and Failing Tickets), verify that both templates render the same fields specified in the plan's acceptance criteria. In F26, UntestedTicket correctly showed all four fields; FailingTicket was missing assigneeName and storyPoints in the template even though the TypeScript interface had them.
- **Plan deviations in implementation.md must be documented even when functionally equivalent**: When the developer places behavior in the view rather than the store (or vice versa), and the plan specified the store, this is a deviation regardless of functional equivalence. The implementation.md "Deviations" section is the contract — "None" should only appear when every plan instruction is followed exactly.
