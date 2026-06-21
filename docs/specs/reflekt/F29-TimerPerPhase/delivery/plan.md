# Timer per Phase

**Feature Spec:** `docs/features/TimerPerPhase/spec.md`

## Context

Retros run long without structure. This feature adds an optional countdown timer per timed phase (Writing, Voting, Discussing), visible to all participants, controlled by the Scrum Master. The timer is a facilitation aid — it signals pacing, not enforces it. Timer state is ephemeral (server memory only), synchronized via SignalR, and cleared on phase advance.

**Services impacted:** Reflekt (sole service). Changes span Domain, Persistence, API (hub + event handlers), and Frontend (stores, composables, components).

## Scope

**In scope:**
- Three new persisted timer default fields on Sprint (WritingTimerDefault, VotingTimerDefault, DiscussingTimerDefault)
- Sprint creation form gains timer duration inputs
- All sprint GET responses include timer defaults
- Ephemeral in-memory timer state service with start/pause/resume/reset/addTime operations
- SprintHub gains five timer hub methods (SM-only) + TimerUpdated/TimerSnapshot events
- Server-side expiry detection (fires TimerUpdated with expired state)
- Timer cleared on phase advance
- TimerSnapshot sent on client join/rejoin
- Frontend timer display in RetroPhaseHeader Row 1 (all participants)
- SM timer controls in a compact popover
- Participant mute toggle (localStorage)
- Audio chime on expiry + red flash animation

**Out of scope (per spec):**
- Auto-advance on timer expiry
- Per-participant timers
- Persistent timer history
- Timer for Grouping or Reporting
- Editable timer defaults after sprint creation
- Custom +time increments (fixed: +1, +2, +5)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | Sprint aggregate: 3 new properties, factory method extension | |
| 2 | persistence-patterns | Follow | SprintConfiguration: 3 new columns, migration | |
| 3 | create-feature | Follow | CreateSprint command/validator/endpoint modification | |
| 4 | (none) | — | Ephemeral TimerService — no skill covers in-memory singleton state | Log |
| 5 | (none) | — | SprintHub timer methods — no skill covers hub method additions | Log |
| 6 | create-domain-event-handler | Follow | BroadcastPhaseChangedHandler modification (clear timer on advance) | |
| 7 | vue-patterns | Follow | useTimer composable | |
| 8 | pinia-patterns | Follow | timerStore | |
| 9 | vue-component-architecture | Follow | TimerDisplay + TimerControls components | |
| 10 | vue-patterns | Follow | RetroPhaseHeader integration + CreateSprintForm modification | |
| 11 | vue-patterns | Follow | useSprintHub timer event wiring | |

## Domain Model Changes

**Sprint (modified):**
- `WritingTimerDefault` — `int`, required, default 5. Range 1-60.
- `VotingTimerDefault` — `int`, required, default 3. Range 1-60.
- `DiscussingTimerDefault` — `int`, required, default 10. Range 1-60.
- Factory method `Sprint.Create(...)` gains three optional parameters with system defaults.

**No new aggregates, no new domain events.** Timer state is ephemeral and not modeled in the domain. Phase advance already raises `PhaseAdvancedEvent` — the timer clearing hooks into the existing handler.

## Data Model Changes

**Sprints table** gains three columns:

| Column | Type | Nullable | Default |
|--------|------|----------|---------|
| WritingTimerDefault | INTEGER | NOT NULL | 5 |
| VotingTimerDefault | INTEGER | NOT NULL | 3 |
| DiscussingTimerDefault | INTEGER | NOT NULL | 10 |

Non-breaking migration — existing rows get defaults.

## Implementation Steps

### Step 1: Sprint domain — add timer default properties

Follow `domain-patterns`.

**Modify:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` — add three properties:
  - `WritingTimerDefault` (int, required init, default 5)
  - `VotingTimerDefault` (int, required init, default 3)
  - `DiscussingTimerDefault` (int, required init, default 10)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — extend `Sprint.Create(...)` factory method with three optional `int?` parameters (`writingTimerDefault`, `votingTimerDefault`, `discussingTimerDefault`). Apply range guard (1-60) for each when provided, fall back to system defaults (5, 3, 10) when null.

**Add a domain helper** on Sprint to retrieve the default duration for the current phase:
- `GetTimerDefaultForPhase()` returns the appropriate default based on `CurrentPhase`. Throws for Grouping/Reporting (non-timed phases).

**Accept:** Sprint has three timer default properties. Factory validates ranges. `GetTimerDefaultForPhase()` returns correct default per phase.

**Depends on:** nothing.

### Step 2: Persistence — EF configuration + migration

Follow `persistence-patterns`.

**Modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — configure the three new `int` columns: `IsRequired()`, `HasDefaultValue(5)` / `HasDefaultValue(3)` / `HasDefaultValue(10)`.

**Create:**
- EF migration via `dotnet ef migrations add AddTimerDefaults -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API`

**Accept:** Migration creates three non-null integer columns with defaults. Existing sprint rows receive defaults. `dotnet ef database update` succeeds.

**Depends on:** Step 1.

### Step 3: API — update sprint creation + responses

Follow `create-feature`.

**Modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs`:
  - Add `int? WritingTimerDefault`, `int? VotingTimerDefault`, `int? DiscussingTimerDefault` to the command record.
  - Add validation rules: when provided, must be `InclusiveBetween(1, 60)`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs`:
  - Pass the three timer values through to `Sprint.Create(...)`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs`:
  - Add `int WritingTimerDefault`, `int VotingTimerDefault`, `int DiscussingTimerDefault` to `SprintResponse`.
  - Add the same three fields to `SprintSummaryResponse`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs`:
  - Include the three timer default fields in the response construction.

**Accept:** POST /api/teams/{teamId}/sprints accepts optional timer defaults. Omitting them uses system defaults. Values outside 1-60 return 400. All GET sprint endpoints return the timer default fields.

**Depends on:** Steps 1, 2.

### Step 4: API — ephemeral TimerService

No matching skill. Describe what + acceptance criteria.

**Create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/Timer/TimerService.cs` — a singleton service managing ephemeral per-sprint timer state.

**Responsibilities:**
- Track timer state per sprint: status (Running, Paused, Expired, Stopped), remaining seconds, end time (UTC, when running).
- Operations: `Start(sprintId, durationSeconds)`, `Pause(sprintId)`, `Resume(sprintId)`, `Reset(sprintId, durationSeconds)`, `AddTime(sprintId, minutes)`, `ClearTimer(sprintId)`, `GetSnapshot(sprintId)`.
- State transition validation: Start requires Stopped state. Pause requires Running. Resume requires Paused. Reset works from Running/Paused/Expired. AddTime works from Running/Paused/Expired.
- Server-side expiry detection: when a timer is started or resumed, schedule a callback (e.g., `System.Threading.Timer`) that fires at the expected end time. On expiry, set status to Expired and invoke a callback (provided at registration) so the hub can broadcast `TimerUpdated`.
- Thread-safe (ConcurrentDictionary + lock per sprint, similar pattern to ReadinessState/ConnectionState in SprintHub).

**Register** as singleton in DI.

**Accept:** TimerService correctly manages state transitions. Start begins countdown. Pause freezes. Resume continues. Reset returns to given duration (Paused). AddTime extends. Server detects expiry and invokes callback. ClearTimer removes all state for a sprint. GetSnapshot returns current state. Invalid transitions throw. Thread-safe under concurrent access.

**Depends on:** nothing (no domain dependency — operates on sprint IDs and durations only).

### Step 5: SprintHub — timer hub methods + events

No matching skill for hub method additions.

**Modify:**
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`

**Add five hub methods** (all SM-only, reject non-SM callers with `HubException`):

| Method | Validation | TimerService call | Broadcast |
|--------|-----------|-------------------|-----------|
| `StartTimer(sprintId)` | SM check, phase must be Writing/Voting/Discussing, timer must be Stopped | `Start(sprintId, sprint.GetTimerDefaultForPhase())` | `TimerUpdated` to group |
| `PauseTimer(sprintId)` | SM check, phase timed, timer must be Running | `Pause(sprintId)` | `TimerUpdated` to group |
| `ResumeTimer(sprintId)` | SM check, phase timed, timer must be Paused | `Resume(sprintId)` | `TimerUpdated` to group |
| `ResetTimer(sprintId)` | SM check, phase timed, timer must be Running/Paused/Expired | `Reset(sprintId, sprint.GetTimerDefaultForPhase())` | `TimerUpdated` to group |
| `AddTime(sprintId, minutes)` | SM check, phase timed, timer Running/Paused/Expired, minutes in {1,2,5} | `AddTime(sprintId, minutes)` | `TimerUpdated` to group |

**SM detection:** The hub already has guest-token auth. SM detection requires checking if the caller's identity matches the team owner. The hub needs `SprintRepository` and `TeamRepository` injected (or a lightweight lookup). Use the same pattern as existing hub methods that validate claims — check for a `scrumMaster` claim or fetch team ownership. The `isScrumMaster` flag is available in the SprintSummary context; the hub should validate by loading the sprint's team and comparing `OwnerId` to the cookie-auth user ID. Since the hub currently only supports guest-token auth, the SM also connects with a guest token but has an additional cookie. The simplest approach: add `isScrumMaster` as a claim on the guest token when the SM joins, set by the JoinSprint endpoint. The hub checks this claim. Alternatively, accept an additional auth scheme on the hub. **Developer decides the mechanism** — the acceptance criteria is that only the SM can invoke timer methods.

**TimerSnapshot on join:** Modify the existing `JoinSprint` method to also send a `TimerSnapshot` to the joining client (similar to `ReadinessSnapshot`). Call `TimerService.GetSnapshot(sprintId)` and send via `Clients.Caller.SendAsync("TimerSnapshot", snapshot)`.

**Expiry broadcast:** When `TimerService` detects expiry, it needs to broadcast `TimerUpdated` to the sprint group. Pass `IHubContext<SprintHub>` to TimerService (or use a callback pattern) so it can push to `sprint:{sprintId}` group on expiry.

**Accept:** All five hub methods work, enforce SM-only access, validate phase and state, broadcast `TimerUpdated` to the sprint group. `JoinSprint` sends `TimerSnapshot`. Server-detected expiry broadcasts `TimerUpdated` with `expired` state. Non-SM callers receive `HubException`. Calls during Grouping/Reporting receive `HubException`. Invalid state transitions (e.g., pause when paused) receive `HubException`.

**Depends on:** Steps 1, 4.

### Step 6: Clear timer on phase advance

Follow `create-domain-event-handler` (modifying existing handler).

**Modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs`:
  - Inject `TimerService`.
  - After clearing readiness, call `TimerService.ClearTimer(sprintId)`.
  - Broadcast `TimerUpdated` with `stopped` state to the sprint group (so clients know the timer was cleared).

**Accept:** Advancing the phase clears any active timer. All clients receive `TimerUpdated` with `stopped` state. No additional warning dialog needed (spec rule 7).

**Depends on:** Steps 4, 5.

### Step 7: Frontend — useTimer composable

Follow `vue-patterns`.

**Create:**
- `web/reflekt-web/src/composables/useTimer.ts`

**Responsibilities:**
- Reactive state: `timerState` (running/paused/expired/stopped), `remainingSeconds`, `endTimeUtc`.
- Client-side countdown: when state is `running`, start a `setInterval(1000)` that decrements `remainingSeconds` based on `endTimeUtc - now`. When the interval detects remaining <= 0, set local state to expired, play audio chime (unless muted), trigger red flash CSS class.
- `applyUpdate(payload)` — called when `TimerUpdated` or `TimerSnapshot` arrives. Sets state, remainingSeconds, endTimeUtc. Starts or stops the interval accordingly.
- `isMuted` — reactive ref backed by `localStorage` key `reflekt:timer:muted`. Toggle method.
- `formattedTime` — computed, returns `MM:SS` string.
- Audio: load a short chime sound (embed as a small base64 audio or a static asset in `public/`). Play on expiry transition unless muted.
- Cleanup: clear interval on unmount.

**Accept:** Composable tracks timer state. Countdown is accurate (computed from endTimeUtc, not incremental). Mute preference persists in localStorage. Audio plays on expiry. Formatted time displays correctly. Interval cleaned up on unmount.

**Depends on:** nothing (pure frontend, no backend dependency).

### Step 8: Frontend — timer store (Pinia)

Follow `pinia-patterns`.

**Create:**
- `web/reflekt-web/src/stores/timer.ts`

**This store is thin** — it primarily holds the reactive timer state and delegates to the `useTimer` composable for countdown logic. It provides actions that the hub event handlers call:

- `applyTimerUpdate(payload)` — delegates to composable.
- `applyTimerSnapshot(payload)` — delegates to composable.
- `clearTimer()` — resets to stopped state.
- Exposes: `timerState`, `remainingSeconds`, `formattedTime`, `isMuted`, `toggleMute`, `isExpired`, `isRunning`.

**Accept:** Store exposes reactive timer state. Hub events update the store. Components read from the store.

**Depends on:** Step 7.

### Step 9: Frontend — TimerDisplay + TimerControls components

Follow `vue-component-architecture`.

**Create:**
- `web/reflekt-web/src/components/retro/TimerDisplay.vue` — the countdown display visible to all participants.
  - Shows `MM:SS` when running or paused.
  - Shows `00:00` with red flash animation (CSS `animate-pulse` or custom keyframes with red text) when expired.
  - Shows nothing when stopped and user is not SM.
  - SM sees the phase default duration (e.g., "5:00") with a start button when stopped.
  - Participant sees a mute/unmute icon toggle.

- `web/reflekt-web/src/components/retro/TimerControls.vue` — SM-only popover with timer action buttons.
  - Visible only to SM, triggered by clicking the timer area.
  - Renders as a compact popover (absolute positioned, similar pattern to readiness tooltip in RetroPhaseHeader).
  - Buttons depend on timer state:
    - Stopped: Start button.
    - Running: Pause, Reset, +1/+2/+5 buttons.
    - Paused: Resume, Reset, +1/+2/+5 buttons.
    - Expired: Reset, +1/+2/+5 buttons.
  - Each button invokes the corresponding hub method via `getHubConnection()!.invoke(...)`.

**Accept:** TimerDisplay renders correctly for all states. TimerControls shows only to SM. Popover opens/closes cleanly. Hub methods are invoked on button click. Red flash animation on expiry. Mute toggle works.

**Depends on:** Steps 7, 8.

### Step 10: Frontend — integrate timer into RetroPhaseHeader + CreateSprintForm

Follow `vue-patterns`.

**Modify:**
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue`:
  - Import and render `TimerDisplay` in Row 1, between the badges and the advance button.
  - Only render during Writing, Voting, Discussing phases.
  - Pass `isScrumMaster` prop to control SM-specific rendering.
  - Pass the sprint's timer defaults (from sprintStore.currentSummary) so the display can show the phase default duration when stopped.

- `web/reflekt-web/src/views/RetroRoomView.vue`:
  - Pass timer-related props to RetroPhaseHeader (the timer defaults from sprintStore.currentSummary).

- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue`:
  - Add three number inputs below the existing column configuration section: Writing Timer (default 5), Voting Timer (default 3), Discussing Timer (default 10).
  - Validate range 1-60 on submit.
  - Include in the emitted `create` payload: `writingTimerDefault`, `votingTimerDefault`, `discussingTimerDefault`.

- `web/reflekt-web/src/stores/sprint.ts`:
  - Add `writingTimerDefault`, `votingTimerDefault`, `discussingTimerDefault` to `Sprint` and `SprintSummary` interfaces.
  - Add the three fields to `CreateSprintPayload`.

**Accept:** Timer area appears in top bar during timed phases. Not shown during Grouping/Reporting. Sprint creation form has timer duration inputs with correct defaults and validation. Sprint store interfaces include timer default fields.

**Depends on:** Steps 3, 8, 9.

### Step 11: Frontend — wire SignalR timer events in useSprintHub

Follow `vue-patterns`.

**Modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts`:
  - Import the timer store.
  - Register `connection.on('TimerUpdated', ...)` — calls `timerStore.applyTimerUpdate(payload)`.
  - Register `connection.on('TimerSnapshot', ...)` — calls `timerStore.applyTimerSnapshot(payload)`.

**Accept:** TimerUpdated events update the timer display for all clients in real-time. TimerSnapshot on join/rejoin renders the correct timer state immediately. Phase advance clears the timer display.

**Depends on:** Steps 5, 8.

## Migration Notes

```bash
# Add migration (from repo root)
dotnet ef migrations add AddTimerDefaults -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API

# Apply migration
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

Migration adds three non-nullable integer columns with defaults. Existing data is unaffected.

## Testing Strategy

**Domain:**
- `Sprint.Create()` with explicit timer defaults — values are set.
- `Sprint.Create()` without timer defaults — system defaults (5, 3, 10) applied.
- `Sprint.Create()` with out-of-range values (0, 61) — throws.
- `Sprint.GetTimerDefaultForPhase()` returns correct value per phase; throws for Grouping/Reporting.

**TimerService:**
- Start → state is Running, endTime is set.
- Pause running → state is Paused, remainingSeconds preserved.
- Resume paused → state is Running, new endTime computed.
- Reset from any active state → state is Paused, remainingSeconds = default duration.
- AddTime running → endTime extended, state stays Running.
- AddTime paused → remainingSeconds increased, state stays Paused.
- AddTime expired → state becomes Running, new countdown from added minutes.
- Invalid transitions throw (pause when paused, start when running, etc.).
- ClearTimer → state is Stopped, all data removed.
- Expiry callback fires when timer reaches zero.

**Hub methods:**
- Non-SM caller → HubException.
- Call during Grouping/Reporting → HubException.
- StartTimer → broadcasts TimerUpdated with running state.
- JoinSprint → sends TimerSnapshot to caller.
- Phase advance → timer cleared, TimerUpdated with stopped state broadcast.

**Frontend:**
- Timer display shows countdown when running.
- Timer display shows 00:00 with red flash when expired.
- SM sees controls; participant does not.
- Mute toggle persists across sessions (localStorage).
- Reconnect receives snapshot and renders correctly.

## KB Impact

None — timer per phase is a self-contained UI/facilitation feature with no business rules requiring KB documentation beyond the spec itself.

## Open Questions

None.
