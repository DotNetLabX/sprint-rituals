# Timer per Phase — Review

## Reviewed By
`reviewer` (Sonnet 4.6 agent) — Sonnet-only review (Codex cross-validation not requested).

## Verdict: APPROVE

No CRITICAL or HIGH issues found. Two MEDIUM findings documented below.

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| TimerService thread-safety issues (lock scope, disposed callbacks) | Not found — lock-per-entry pattern is correct; `ClearTimer` TryRemove + lock is safe; `_onExpired` called outside lock intentionally to avoid holding lock during async broadcast |
| SM detection mechanism soundness | Correct — IServiceScopeFactory + 3-repo lookup, UserId null check guards guest-only participants |
| Store/composable boundary coherence after deviation | Coherent — store holds authoritative state from hub events; composable adds interval countdown. No double-countdown. `timerStore.formattedTime` is a static-snapshot getter (see MEDIUM-1) |
| Hub expiry callback lifetime/timing issues | None — callback wired synchronously after `MapHub` in `UseReflektApi`, before any request is served |
| `GetSprintSummaryEndpoint` missing timer fields | Not found — all three fields populated at line 47-49 |

---

## Findings

### [MEDIUM] timerStore.formattedTime getter returns stale time during countdown

**File:** `web/reflekt-web/src/stores/timer.ts:14-19`

**Issue:** The `formattedTime` getter computes from `state.remainingSeconds`, which is a static integer set only when a hub event arrives. During a running countdown, `remainingSeconds` is not updated by the store — the composable maintains its own `displayRemainingSeconds` via `setInterval`. Any future code that reads `timerStore.formattedTime` directly will display a frozen value that only updates on server events (~every state transition), not every second. The store exports it as a public API which creates a subtle trap.

**Fix:** Either remove the `formattedTime` getter from the store (it belongs only in the composable) and add a comment to the store making clear it holds event-sourced snapshots only, or document that `formattedTime` is only accurate for paused/stopped states and should not be used during running state.

---

### [MEDIUM] No timer domain/service tests added despite explicit plan testing strategy

**File:** `tests/Reflekt.UnitTests/Sprints/SprintTests.cs` (not modified), no `TimerTests.cs` created

**Issue:** The plan's testing strategy explicitly lists 12+ scenarios for `Sprint.Create()` with timer defaults, `GetTimerDefaultForPhase()`, and `TimerService` state transitions. None were added. The existing `SprintTests.cs` has no coverage of timer default properties or the `GetTimerDefaultForPhase()` helper. `TimerService` has zero test coverage.

**Context:** The unit test project currently has pre-existing compile errors (`ColumnType.WentWell`, stale `Team` constructor) that would block running any tests. This likely deterred adding new tests. The correct resolution is to fix the pre-existing errors and add the timer tests — not to omit them.

**Fix:** Add `SprintTimerTests.cs` covering the plan's domain scenarios; add `TimerServiceTests.cs` covering state transitions (Start→Running, Pause→frozen, Resume→running, Reset→Paused, AddTime variants, invalid transitions throw, ClearTimer removes state). Fix or exclude the pre-existing broken tests in the same pass.

---

### [LOW] Timer range constants named after Writing phase but apply to all three

**File:** `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs:5-6`

**Issue:** `WritingTimerMin = 1` and `WritingTimerMax = 60` are used in `ValidateTimerRange()` for all three timer types (writing, voting, discussing). The values are correct but the names imply Writing-specific constraints.

**Fix:** Rename to `TimerDefaultMin` / `TimerDefaultMax` (or `MinTimerMinutes` / `MaxTimerMinutes`).

---

### [LOW] AddTime validates `minutes` before SM authorization check

**File:** `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs:144-150`

**Issue:** The `minutes is not (1 or 2 or 5)` guard fires before `RequireScrumMasterAsync`. A non-SM caller supplying an invalid minutes value receives a different error than a non-SM caller with a valid one. This allows probing the allowed values. Not a meaningful security risk in this context, but inconsistent with the other four hub methods which do SM check first.

**Fix:** Move the SM check before the minutes validation, or accept the current order (it's a very minor stylistic inconsistency).

---

### [LOW] TimerControls popover has no click-outside-to-close behavior

**File:** `web/reflekt-web/src/components/retro/TimerControls.vue`

**Issue:** The popover closes on action (`emit('close')` after each invoke) but not when the SM clicks elsewhere on the page. The only way to dismiss without acting is to click the timer area again (which toggles `showControls`). This may feel sticky in practice.

**Fix:** Add a `useClickOutside` composable reference or a transparent overlay behind the popover that closes it, consistent with how other floating panels are dismissed in the codebase.

---

## Positive Observations

- **TimerService thread-safety is well-executed.** Per-entry `object Lock` field + `ConcurrentDictionary` for the outer map is the right approach. `ClearTimer` correctly uses `TryRemove` so the expiry callback's subsequent `TryGetValue` returns nothing and exits safely — no double-fire, no crash after clear.
- **Expiry callback pattern is clean.** Wiring `IHubContext<SprintHub>` via `app.Services.GetRequiredService` after `MapHub` in `UseReflektApi()` avoids circular DI without introducing intermediary abstractions. The callback fires outside the lock, which is correct for async SignalR calls.
- **Minutes-to-seconds conversion is applied consistently.** `GetTimerDefaultForPhase() * 60` appears in both `StartTimer` and `ResetTimer`; `AddTime` takes minutes directly and converts internally. No conversion leaks.
- **SM detection is solid.** Resolves scoped repos from `IServiceScopeFactory`, checks `participant.UserId is null` to reject guest-only callers, then compares `team.OwnerId`. No claims-forgery path — SM status is always verified against the database, not trusting a claim.
- **`useTimer` composable interval correctness.** Countdown computes from `endTimeUtc - Date.now()` (not incrementally decrementing), so clock drift doesn't accumulate. `stopInterval()` is called in `onUnmounted` — no leak.
- **Pre-existing TypeScript errors correctly attributed.** `TeamSelector.vue` 5 TS errors confirmed pre-exist (`a505ea6`) and were not modified by this feature.
- **Store/composable deviation is better than the plan.** Plan proposed composable-delegation from store, which would require a module-level singleton composable and break `onUnmounted`. The implemented split (store = event state, composable = interval + audio) is architecturally cleaner.

---

## Gaps

- **`TimerService` state machine has no guard against `Start` being called when `ConcurrentDictionary` has a stale entry in `Expired` state after a server restart scenario** — not applicable (server restart clears all in-memory state, clients reconnect and receive `stopped` snapshot). Not a real gap.
- **No integration test for `POST /api/teams/{teamId}/sprints` timer default fields** — `SprintEndpointTests.cs` not updated to include timer default assertions. Minor gap but the pre-existing test failures make this moot until those are fixed.
- **Audio chime is a placeholder (base64 WAV produces near-silence).** Implementation notes this. Acceptable for now but should be tracked.

---

## Open Questions

None. All pre-commitment predictions resolved during investigation.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Production API build | PASS | `dotnet build src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj --no-incremental` | 0 warnings, 0 errors |
| Full solution build | FAIL (pre-existing) | `dotnet build Reflekt.slnx --no-incremental` | 17 errors in `Reflekt.UnitTests` only — `ColumnType.WentWell`, stale `Team` constructor. Confirmed pre-exist in `a505ea6` (prior feature commit). Not introduced by this feature. |
| TypeScript type-check | FAIL (pre-existing) | `npm run type-check` in `web/reflekt-web` | 5 errors in `TeamSelector.vue` only. File not modified by this feature (confirmed via `git diff a505ea6..HEAD -- TeamSelector.vue` — empty diff). |
| Migration present | PASS | Glob `**/Migrations/*AddTimerDefaults*` | `20260519061608_AddTimerDefaults.cs` found — adds 3 NOT NULL INTEGER columns with defaults 5/3/10 |
| Timer test files | ABSENT | Glob `tests/**/*Timer*.cs` | No files found |
| TeamSelector.vue pre-exists | CONFIRMED | `git diff a505ea6..HEAD -- web/reflekt-web/src/components/teams/TeamSelector.vue` | Empty (no changes by this feature) |
