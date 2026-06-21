# Daily Developer Progress — Review

## Reviewed By
reviewer (Sonnet 4.6 agent) — Step 2 code review, Cycle 1/3 re-review

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Result |
|-----------|--------|
| Stall detection business day counting edge cases | Correctly implemented — excludes transition day, counts Mon-Fri only |
| SignalR broadcast error handling (catch scope, logger injection) | Correct — try/catch wraps only broadcast call, logger injected via constructor |
| Frontend store cleanup: SignalR lifecycle gaps | One MEDIUM: stopSignalR not awaited in switchTab |
| Sprint selector disable CSS approach | Correctly applied `opacity-50 pointer-events-none` to BaseSelect element |
| Daily breakdown computation / cumulative ordering | Correct — running cumulative sum, completionDay clamped to currentDay |

## Findings

### [MEDIUM] stopSignalR not awaited in switchTab
**File:** `client/src/stores/developersStore.ts:119`
**Issue:** `dailyProgressStore.stopSignalR()` is called without `await` inside `async function switchTab`. The function is async, so the promise is silently dropped. In practice the disconnect runs asynchronously in the background, but a rapid switch back to dailyProgress before the stop resolves could briefly create two simultaneous connections.
**Fix:** Change line 119 to `await dailyProgressStore.stopSignalR()`.

### [MEDIUM] Daily Progress tab blocked when no closed sprints are synced (open question)
**File:** `client/src/views/DevelopersView.vue:158-170`
**Issue:** The tab bar renders only inside the `v-else` block that requires `store.sprints.length > 0`. `store.sprints` is populated by `getSprints()`. If `getSprints()` only returns closed sprints, a team with only an active sprint (never closed one) would see the "No developer data yet" EmptyState and have no path to the Daily Progress tab — even though an active sprint exists and the tab is spec'd as always visible. The spec's Flow 6a handles "no active sprint" on the tab itself; the view-level guard should not block it.
**Confidence:** MEDIUM — depends on whether `getSprints()` includes the active sprint in its response. If it does, `sprints.length > 0` is satisfied and this issue does not manifest. Raised as an open question.

### [LOW] paceGapSp is null for zero-assigned-SP developers outside grace period
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs:154`
**Issue:** The guard `if (!isGracePeriod && dailyPace > 0)` leaves paceGapSp as null when dailyPace == 0 (zero assigned SP) even after the grace period ends. The spec (BR13) only requires null during grace period; it is silent on the zero-SP case. The behavior is defensively correct (isBehindPace is always false when dailyPace == 0 per BR26, so the gap value is never surfaced in the UI), but the field semantics differ from the spec's definition for non-grace-period state.
**Fix:** None required for correctness. A code comment noting the intentional null for the zero-dailyPace case would be sufficient.

### [LOW] onUnmounted stopSignalR is fire-and-forget
**File:** `client/src/views/DevelopersView.vue:79-83`
**Issue:** Vue 3's `onUnmounted` is synchronous — `dailyProgressStore.stopSignalR()` cannot be awaited. The connection cleanup runs asynchronously after the component is torn down. This is idiomatic Vue 3 and the SignalR library handles graceful disconnection internally.
**Fix:** None needed.

## Positive Observations

- **Business logic accuracy**: All plan business rules (BR1-27) are faithfully implemented. The three-stage developer filtering (exclusion → active → sub-team) matches the plan exactly.
- **CountBusinessDays is correct**: Starts from `from.Date.AddDays(1)` (day after transition), loops while `<= to.Date`, skips Saturday/Sunday. The stall threshold (`businessDays <= 2` continues) correctly means "more than 2 full business days must have elapsed."
- **Grace period suppression is complete**: paceGapSp is null, isBehindPace is false, and alerts list is empty during grace period — all three signals suppressed as specified.
- **SignalR broadcast placement**: Broadcast fires after `SyncSprintsFromJiraAsync` returns (data persisted), before `SendOkAsync` (response not yet sent). Correct ordering per plan.
- **Test coverage**: 9 test slices cover null sprint, basic pace + breakdown, zero SP, grace period, behind-pace detection, stall detection, bug SP inclusion, sub-team filter, and excluded developer IDs. Tests are clean and follow the existing TestData builder pattern.
- **Help tooltips**: All 10 tooltip entries from `help.tooltips.md` are wired to matching elements in `DeveloperProgressCard.vue` and `DailyProgressTab.vue`. Tooltip text matches the source file.
- **Architecture doc amendment**: SignalR removed from out-of-scope list, new section added under Cross-Cutting Concerns with accurate description, gap entry logged for future `signalr-patterns` skill.
- **Sprint.AddMembership deviation**: Correctly placed in `Sprint/Behaviors/Sprint.cs` following the partial class convention. Justification (private backing collection inaccessible from test fixtures) is clear and sound.
- **URL sync**: `daily-progress` ↔ `'dailyProgress'` mapping is symmetric in both `onMounted` tab parser and the `watch` callback.
- **KB updates**: `docs/kb/analytics/daily-progress.md`, `docs/kb/frontend-map.md`, `docs/kb/cross-cutting.md`, and `docs/kb/index.md` are all updated per the plan's KB Impact section.

## Gaps

- **No test for stall boundary (exactly 2 business days)**: The production code correctly uses `<= 2` (not stalled), but there is no test pinning the boundary. The threshold could silently regress.
- **No test for completionPercent > 100%**: BR25 specifies this is valid and must not be capped. The production code allows it, but no test verifies it.
- **SignalR reconnect without sync event**: If the connection drops and reconnects while the user is on the tab, `onreconnected` sets `signalRConnected = true` but does NOT re-fetch data. The view may be momentarily stale until the next `SprintSynced` event arrives. Minor gap — no plan requirement for reconnect-triggered refresh.

## Open Questions

- **Does `getSprints()` include the active sprint?** If yes, the MEDIUM finding about the tab being blocked for teams with no closed sprints does not apply. If no, the Daily Progress tab is inaccessible until at least one sprint is closed and synced — which contradicts the spec requirement that the tab is always visible.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build src/Fokus.slnx --configuration Release --no-incremental` | Build succeeded, 13 warnings (pre-existing in QaWorkloadService.cs), 0 errors |
| Tests | PASS | `dotnet test src/Fokus.slnx --configuration Release --no-build` | Failed: 0, Passed: 9, Skipped: 0, Duration: 375ms |
| SignalR DI | PASS | Read `DependencyInjection.cs:25` | `services.AddSignalR()` present |
| Hub mapping | PASS | Read `Program.cs:50` | `app.MapHub<SprintHub>("/hubs/sprint")` present, after UseFokusMiddleware, before MapFallbackToFile |
| Hub authorize | PASS | Read `SprintHub.cs:11` | `[Authorize]` attribute on hub class |
| Broadcast try/catch | PASS | Read `SyncSprintsEndpoint.cs:44-54` | Broadcast wrapped in try/catch with LogWarning, placed after sync, before SendOkAsync |
| TypeScript types | PASS | Grep `types/index.ts` | All 7 Daily Progress types present, match backend record shapes |
| Tooltip wiring | PASS | Read `DeveloperProgressCard.vue`, `DailyProgressTab.vue`, `help.tooltips.md` | All 10 tooltip texts wired to matching elements via `title` attributes |
| selectSprint/selectLastN guards | PASS | Read `developersStore.ts:55,75` | Early return `if (activeTab.value === 'dailyProgress') return` at top of both functions |
| PageToolbar disabled prop | PASS | Read `PageToolbar.vue:23,127` | `disabled?: boolean` prop with default false; applied as class to BaseSelect |
