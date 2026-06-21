# UI Improvements — Review

## Reviewed By
`reviewer` (Sonnet agent, claude-sonnet-4-6)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **Ticket count filter alignment with SP logic** — Expected a possible divergence (e.g., starting ticket count using different guards than SP). Actual: filters are identical; each ticket-count LINQ expression mirrors its SP twin with `.Count()` replacing `.Sum()`. No divergence found.
2. **`SaveAsync` concurrent-write risk from multiple per-section saves** — Expected possible clobbering when two endpoints update AppSettings simultaneously. Actual: `AppSettingsRepository.SaveAsync` loads-updates-saves in a single async flow; SQLite serializes writes. Low real-world risk.
3. **Missing `excludedFromScopeStatuses` in frontend `AppSettings` type** — Expected this could be absent since the plan splits it out. Actual: confirmed absent by design — `excludedStatuses` is managed independently via its own endpoint and local `ref` in SettingsView, not through `store.settings`. Correct.
4. **`syncAll` deviation from plan** — Plan says "update `syncAll` to call `saveSyncConfig` instead of `save()`". Implementation also calls `saveBoardAction` (not in plan). Documented in implementation.md Key Decisions as intentional. The addition is narrowly scoped and correct — `syncAll` needs the board ID persisted. Acceptable.
5. **Shared `store.error` ref across all save actions** — Single `error` ref shared by all 6 store actions creates a fragile error-propagation path in SettingsView panels. See finding below.

---

## Findings

### [MEDIUM] Shared store error ref creates fragile error detection in save panels

**File:** `client/src/stores/settingsStore.ts:33` and `client/src/views/SettingsView.vue:47`

**Issue:** The store has a single `error` ref and a single `saving` ref shared across all 6 save actions (`saveBoardAction`, `saveDoneStatusesAction`, etc.). Each SettingsView panel calls `await store.saveXxxAction(...)` and then checks `if (!store.error)` to decide whether to show success. This check is fragile:

1. Each action sets `error.value = null` at the start. If a second action is in flight when the first one completes, the second action's `error.value = null` could clear the first action's error before the view reads it.
2. Conversely, a stale `store.error` from a prior unrelated action could suppress a success flash even when the current action succeeded.

In practice this only manifests with concurrent saves (unlikely in the current UI), but the pattern is architecturally fragile.

**Fix:** Each store action should `throw` on error rather than set a shared ref, and the view's `try/catch` block should catch it. Alternatively, each action should return a `{ success, error }` result object. The existing local per-panel `boardError`, `doneStatusesError`, etc. refs in SettingsView are already wired for this — they just need the store to propagate errors via `throw` instead of a shared ref.

Example for `saveBoardAction`:
```ts
async function saveBoardAction(boardId: number | null) {
  saving.value = true
  try {
    await saveBoard(boardId)
    settings.value = { ...settings.value, boardId }
  } catch (e) {
    throw e  // let caller handle
  } finally {
    saving.value = false
  }
}
```
Then `SettingsView` `saveBoardPanel` catch block handles it — which it already does via `catch (e: any) { boardError.value = e.message }`.

---

### [LOW] `BurnupChart.vue` tooltip uses `theme: 'dark'` alongside custom formatter

**File:** `client/src/components/sprints/BurnupChart.vue:71`

**Issue:** The `tooltip` object has both `theme: 'dark'` and `custom: (...)`. ApexCharts ignores `theme` when a `custom` formatter is provided (the custom HTML takes full control). The `theme: 'dark'` property is dead code.

**Fix:** Remove `theme: 'dark'` from the tooltip config since the custom formatter renders its own styled HTML.

---

### [LOW] `weightsSum` is a function, not a computed

**File:** `client/src/views/SettingsView.vue:510`

**Issue:** `const weightsSum = () => form.healthWeights.completion + form.healthWeights.disruption + form.healthWeights.carryOver` is a plain function. Since `form` is `reactive`, calling `weightsSum()` in the template will reactively update, so this works correctly. But the codebase elsewhere uses `computed()` for derived reactive values, and using a plain function here is inconsistent with the pattern and slightly less efficient (recalculates on every render rather than caching).

**Fix:** `const weightsSum = computed(() => ...)` and update template to `weightsSum.value` or bind with `:class="weightsSum === 100 ? ..."`.

---

## Positive Observations

- **Backend endpoints are an excellent reference-pattern match.** All 6 new endpoints follow `SaveExcludedStatusesEndpoint` precisely: `AppSettingsRepository` injection, load → mutate → `SaveAsync` → respond. Validators are sibling classes in the same file. Authorization attribute on every endpoint. No deviation.
- **`SaveHealthConfigEndpoint` validator goes beyond the plan.** The plan specifies "validates weights sum == 100". The implementation also validates threshold ordering (amber < green for completion, amber > green for disruption/carry-over) and `InclusiveBetween(0,100)` for each field. This is strictly better than the minimum requirement.
- **Ticket count accumulators are exact mirrors of SP accumulators.** The four LINQ expressions (starting committed, starting bug, per-day additions/removals/completions) use identical filters — only the terminal operator changes from `.Sum(m => m.GetEffectiveSp(...)!.Value)` to `.Count(m => ...)`. The `bugCompletedTicketsToday` filter (line 516–521) correctly uses the same `doneTransitionByTicket` lookup, `RemovedAt == null`, and excluded-status guard.
- **Tooltip custom formatter is type-safe.** Accessing `props.burnupData[dataPointIndex]` directly from the Vue props rather than extracting values from the ApexCharts series is the correct approach — avoids serialisation rounding and keeps full type access.
- **Sort state design is appropriate.** Local `ref` state per component (not store), shared across single/multi modes within the same component so sort persists on mode switch. This matches the plan spec and is a good UX decision documented in Key Decisions.
- **`SettingsView` tab wrapping uses `<template v-if>` not `<div v-if>`.** Avoids injecting extra DOM wrapper elements that would break section spacing. Correct.
- **Users tab button correctly gated with `v-if="authStore.isAdmin"`.** Matches the existing admin-only guard pattern throughout the view.
- **`syncAll` saves only the two fields it actually consumes** (`syncConfig` + `board`) rather than all settings. This avoids side-effects on sections the user may not have touched. Explicitly called out in Key Decisions.
- **Old blob `SaveSettingsEndpoint` and `SaveSettingsCommand` fully removed** with no remaining consumers — confirmed via grep returning no matches.
- **`BugRatioDevTable` sort value function uses `any` type appropriately.** The function accepts developer entries from both multi and single response shapes which share the relevant fields — a pragmatic typing choice that avoids duplicating logic.

---

## Gaps

- **No frontend validation on `SaveBoard` when board is unset.** The backend accepts `BoardId: null` (nullable `int?`), the validator only fires `GreaterThan(0)` when the value is present. The frontend "Save Board" button has no disabled state when `form.boardId === null`. A user can save `null` as the board ID. This is intentional (clearing the board is valid), but there is no UI hint that null means "no board configured."
- **No guard on `sortedSingleSprintRows` when `bd` is undefined.** `sortedSingleSprintRows` sorts by `a.bd?.spAssigned ?? 0`, which defaults undefined breakdown to 0. A developer row with no breakdown for the selected sprint (the `v-else` colspan-6 dash row) would sort as 0 across all columns. This is cosmetically acceptable but means undefined-breakdown developers always sort to the bottom in ascending mode regardless of column. No data loss, just a minor UX quirk.
- **Health tab layout: Health Thresholds and Health Weights are in separate `<section>` elements** but share a single "Save Health Config" button in the weights section. This means a user editing only thresholds would not see a save button until they scroll to the weights section. The plan describes them as "one panel" with one save button — the implementation puts the save button at the bottom of the weights section, which works, but the UX could be clearer.

---

## Open Questions

None — all CRITICAL and HIGH findings were self-audited away. The MEDIUM finding on shared store error ref is genuine but low-impact enough (sequential single-user saves only) to not block merge. The LOWs are cosmetic.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build (compile check) | **pass** | `dotnet build Fokus.API.csproj --no-restore -v q -o _build_check` | `Build succeeded. 0 Error(s)` |
| `SaveSettingsCommand` removed | **pass** | `grep -r SaveSettingsCommand src/` | No matches |
| `SaveSettingsEndpoint` removed | **pass** | `grep -r SaveSettingsEndpoint src/` | No matches |
| 6 new endpoints exist | **pass** | Directory check | SaveBoard, SaveDoneStatuses, SaveWorkflowStages, SaveHealthConfig, SaveBugRatioAlerts, SaveSyncConfig — all present |
| All 6 endpoints have `[Authorize(Roles = "Admin")]` | **pass** | Code read | Confirmed on each file |
| All 6 endpoints have validators | **pass** | Code read | Sibling validator classes in each endpoint file |
| Ticket count filters match SP filters | **pass** | Code read ScopeChangeService.cs:427–521 | All 4 LINQ expressions mirror SP equivalents |
| BurnupDataPoint record updated | **pass** | Code read ScopeChangeService.cs:54–63 | `TotalScopeTickets`, `CompletedTickets`, `BugTickets` added |
| Frontend BurnupDataPoint interface updated | **pass** | Code read types/index.ts:334–344 | 3 new fields present |
| TeamTable changed to `w-auto` | **pass** | Code read TeamTable.vue:50 | `class="w-auto text-sm"` confirmed |
| Sort state local to component | **pass** | Code read DevelopersView.vue:200–252, BugRatioDevTable.vue:26–74 | `ref` state, not store |
| SettingsView tab bar present | **pass** | Code read SettingsView.vue:603–625 | 5 tab buttons, Users gated with `v-if="authStore.isAdmin"` |
| Old `save()` function removed | **pass** | Code read SettingsView.vue | No `save()` function found |
| `syncAll` uses per-section saves | **pass** | Code read SettingsView.vue:544–583 | Calls `saveSyncConfigAction` + `saveBoardAction` |
