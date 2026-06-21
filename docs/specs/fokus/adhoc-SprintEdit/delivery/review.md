# Sprint Edit — Review

## Reviewed By

`reviewer` (Sonnet agent) — Sonnet-only review. Codex cross-validation was not requested.

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| Date serialization to Jira using `"O"` format could be rejected by Jira API | Confirmed as a pre-existing serializer behavior, plan explicitly flagged it, moved to Open Questions |
| `IJiraClient` interface violates no-interface guardrail | Not a violation — Jira module CLAUDE.md defines this as the module contract pattern |
| Debug `Console.WriteLine` calls left in production code | Pre-existing in codebase, not introduced by this feature |
| `onSprintUpdated()` in SprintsView silently drops typed emit arg | Valid Vue 3 pattern — handler correctly triggers refresh without needing the arg |
| Escape key handler on non-focusable overlay div | Works because keyboard events bubble from focused inputs inside the dialog |

## Findings

No CRITICAL or HIGH issues found.

### LOW: Escape key requires focus inside dialog

**File:** `client/src/components/sprints/SprintEditDialog.vue:84`
**Issue:** The `@keydown="onKeydown"` handler is on the backdrop `div`, which is not natively focusable. Escape works when a form field has focus (events bubble), but if the user opens the dialog and clicks outside the form inputs (e.g. on the title or dialog panel background), focus moves to the body and Escape no longer reaches the handler.
**Fix:** Add `tabindex="-1"` to the outer backdrop div and call `.focus()` on open, or add a `@keydown.esc` listener on the inner dialog card div which is always the click target.

### LOW: `requestAnimationFrame`/focus not applied after dialog open

**File:** `client/src/components/sprints/SprintEditDialog.vue:31-43`
**Issue:** When the dialog opens (`watch(open, ...)`), the form is reset but no input is auto-focused. This is a minor UX gap — the name field should receive focus on open so the user can start typing immediately.
**Fix:** Add `nextTick(() => nameInputRef.value?.focus())` in the watch handler, with a template ref on the name input.

## Positive Observations

- **Plan conformance: perfect.** All 9 steps implemented with no deviations. Every file the plan specified was created or modified. The `Deviations from Plan` section correctly reports none.
- **Authorization pattern: correct.** `[Authorize(Roles = "Admin")]` attribute matches every other write endpoint in the codebase. The implementation note about a linter correction was accurate.
- **Jira-first write-through is correct.** UpdateSprintEndpoint calls Jira before touching the local DB — if Jira fails, the local DB is not modified, which is exactly the required behavior.
- **`refreshSprints()` + `fetchAllData()` sequential ordering is correct.** The store action correctly re-fetches the sprint list before re-fetching analytics, ensuring the sprint selector is updated before data queries use the new state.
- **`SprintEditDialog` is clean and idiomatic Vue 3.** `reactive` form, `ref` for saving/error, `watch(open, ...)` for reset, try/catch/finally pattern — all consistent with `SettingsView.vue` as specified.
- **Opt-in `showEditButton` prop** (default `false`) correctly prevents the dialog from loading in the four other views using `PageToolbar`. Conditional render guard `v-if="showEditButton && selectedSprint"` is correct.
- **`toDateInput()` helper** correctly slices ISO string to `yyyy-MM-dd` for `<input type="date">` pre-fill.
- **`goal: form.goal || undefined`** correctly converts empty string to `undefined` so the backend receives `null`/omitted rather than an empty string for an optional goal field.
- **Migration is correct.** `AddSprintGoal` adds a nullable `TEXT` column with `maxLength: 1024` — matches the EF Core config `HasMaxLength(1024)` on the property.
- **`GetClosedSprints` updated correctly.** Both the DTO (`ClosedSprintItem.Goal`) and the endpoint projection (`Goal = s.Goal`) are present. Frontend type and API client updated consistently.

## Gaps

- **Jira date format for write not yet validated.** `JiraDateTimeConverter.Write` uses `value.ToString("O")` format (e.g. `2024-01-15T00:00:00.0000000`) — no timezone component. Jira's Agile API sprint update endpoint typically expects ISO 8601 with timezone (e.g. `2024-01-15T00:00:00.000+0000`). This serializer was originally designed for reading Jira dates and has never been exercised on writes. The plan acknowledged this risk. Needs first manual integration test before this feature is considered fully verified.
- **No client-side validation feedback.** The validator enforces Name not empty and StartDate < EndDate on the backend, but the frontend form submits and waits for a 400 response to display an error. There is no inline `required` attribute or HTML5 validation. Acceptable for v1 per plan scope, but worth noting.

## Open Questions

- **Date serialization format accepted by Jira write API.** Confidence: MEDIUM. The `"O"` format (`2024-01-15T00:00:00.0000000`) is a valid ISO 8601 representation, and some Jira instances accept it. However, Jira's documented examples show timezone-offset format. If the first manual test fails, the fix is to change `JiraDateTimeConverter.Write` to use `value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")` or similar. This is a known unknown per the plan, not a code defect — escalating to Open Questions rather than a finding.

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Build | pass | `dotnet build Fokus.API.csproj` | Build succeeded. 0 Error(s), 2 Warning(s) — NU1903 pre-existing NuGet vulnerability unrelated to this feature |
| Authorization pattern | matches codebase | `grep -r "Authorize(Roles"` | All 13 write endpoints use `[Authorize(Roles = "Admin")]` attribute pattern — matches new endpoint |
| `TeamManagedJiraClient` implements `UpdateSprintAsync` | inherited | read `TeamManagedJiraClient.cs` | Extends `RestApiJiraClient`, inherits `UpdateSprintAsync` — no missing implementation |
| `IJiraClient` guardrail | not violated | read `Jira/CLAUDE.md` | Module contract pattern is intentional — Jira module defines `IJiraClient` as its contract by design |
| Debug `Console.WriteLine` | pre-existing | `grep -r "Console.WriteLine"` | 3 debug lines in `RestApiJiraClient.cs` + `DependencyInjection.cs` — pre-existing, not introduced by this PR |
| `refreshSprints()` in store | present | read `sprintsStore.ts:54-57` | `getClosedSprints()` called, result assigned to `closedSprints.value` |
| Sequential refresh in view | correct | read `SprintsView.vue:77-80` | `await store.refreshSprints()` then `await store.fetchAllData()` — sequential as specified |
| Migration file | present | read `20260510104427_AddSprintGoal.cs` | Nullable `TEXT` column, `maxLength: 1024`, correct `Up`/`Down` |
