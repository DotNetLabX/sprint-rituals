# ActiveSprintAnalytics — Lessons

## Developer Lessons
- When widening a repository query from a single state to multiple states, update every call site in a single pass — grep for the old method name before starting to avoid missing endpoints.
- Active sprint prior-sprint pattern: when the selected sprint is not in the closed-only `ascending` list, `ascending[^1]` (C# index-from-end) gives the most recent closed sprint as prior. This avoids null deltas on active-sprint single views.
- Sparkline anchor for active sprint: use `ascending.Count - 1` as the anchor index (not `selectedIndex`) so all preceding closed sprints remain in the window candidate set.
- Rolling window with active sprint: set `rollingWindowBase = ascending.Count` so the 2-sprint expansion draws from the full closed-sprint history, not an empty set.
- When a DTO previously named `ClosedXxx` gains a `State` field and gets renamed to `Xxx`, the frontend `PageToolbar` can read `sprint.state` directly to add visual indicators — no additional API call needed.
- PowerShell paths with backslashes must be wrapped in `powershell -Command "..."` when invoked from bash; bare `cd D:\...` fails.

## Architect Lessons
- When listing exception endpoints in a plan ("all of X except Y"), verify by grep that the exception is actually justified. The plan excluded GetScopeChange from the error message update list, but it contained the same error string as the other 8 endpoints. Grep-verify exclusion claims before publishing the plan.
- When plan instructions say "if it references X" or "verify Y", require the developer to document the verification result in implementation.md even when no code change was needed. Omitting "checked SyncTab.vue — no references found" creates ambiguity during the done check about whether the step was skipped or checked-and-skipped.
- [TRACKED] Category labels in implementation.md (A/B/C/D) should match the plan exactly. GetTestTimeline was plan-Category B but reported as Category A. When the treatment is identical, this is cosmetic — but it forces the reviewer to investigate whether the mismatch hides a functional gap.

## Reviewer Lessons
- When a plan widens a sprint lookup from "closed only" to "active + closed", verify every endpoint that does a single-sprint lookup independently — not just those that compute averages. The active-sprint 404/500 bugs in `GetQaMetrics` and `GetQaWorkload` were both single-sprint lookup gaps, not averaging gaps.
- In Category C/D endpoints that load the selected sprint from `windowSprints` after building a closed-only window, check whether the active sprint is ever added to the load set before the `.First()` call. If not, it is a guaranteed runtime crash.
- A pre-existing case-sensitivity bug (`'closed'` vs `'Closed'`) in a component becomes reviewable when the rename introduces a new `state` field that is now actually read. Track what fields are newly live after a rename.

## Skill Gaps
- **Missing skill:** A "widen-analytics-endpoint" pattern covering the closed→active+closed repository filter, prior-sprint resolution for active targets, sparkline anchor adjustment, and rolling-window base adjustment. Would benefit any future feature that extends analytics to include in-progress sprints. Reference files: `GetBugRatioEndpoint.cs`, `GetDeveloperQualityEndpoint.cs`, `GetDeveloperThroughputEndpoint.cs`.
