# Timer per Phase — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.API/Features/Sprints/Timer/TimerService.cs` — Singleton ephemeral timer state service: ConcurrentDictionary per sprint, Start/Pause/Resume/Reset/AddTime/ClearTimer/GetSnapshot operations, server-side expiry via System.Threading.Timer, callback-based expiry broadcast wiring
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260519*_AddTimerDefaults.cs` — EF migration adding WritingTimerDefault, VotingTimerDefault, DiscussingTimerDefault columns (NOT NULL with defaults 5, 3, 10)
- `web/reflekt-web/src/composables/useTimer.ts` — Vue composable: watches timerStore state, runs client-side interval countdown from endTimeUtc, triggers audio chime and red flash on expiry, manages mute toggle
- `web/reflekt-web/src/stores/timer.ts` — Pinia options-API timer store: holds authoritative timer state set by hub events, exposes isRunning/isExpired/formattedTime getters, applyTimerUpdate/applyTimerSnapshot/clearTimer/toggleMute actions
- `web/reflekt-web/src/components/retro/TimerDisplay.vue` — Timer countdown display for all participants; SM-clickable to open TimerControls popover; shows phase default when stopped (SM only); mute/unmute toggle; red pulse on expiry
- `web/reflekt-web/src/components/retro/TimerControls.vue` — SM-only popover with state-dependent buttons (Start/Pause/Resume/Reset/+1m/+2m/+5m); invokes hub methods via getHubConnection()

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` — Added three required init properties: WritingTimerDefault, VotingTimerDefault, DiscussingTimerDefault (int)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — Extended Sprint.Create() with three optional int? timer parameters, range validation (1-60), system defaults (5/3/10); added GetTimerDefaultForPhase() helper; added ValidateTimerRange() private method
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — Added IsRequired().HasDefaultValue() configuration for the three timer columns
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs` — Added three optional int? timer fields to command record; added InclusiveBetween(1,60) validation rules
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs` — Passed three timer values through to Sprint.Create()
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — Added WritingTimerDefault, VotingTimerDefault, DiscussingTimerDefault to both SprintResponse and SprintSummaryResponse
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Added three timer fields to SprintSummaryResponse constructor call
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — Injected TimerService and IServiceScopeFactory; added TimerSnapshot on JoinSprint; added five SM-only timer hub methods (StartTimer/PauseTimer/ResumeTimer/ResetTimer/AddTime); added RequireScrumMasterAsync() helper (loads participant+sprint+team to verify OwnerId match); added LoadTimedSprintAsync() helper (rejects Grouping/Reporting)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` — Injected TimerService; on phase advance: ClearTimer + broadcast TimerUpdated(stopped) to sprint group
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — Added TimerService singleton registration; wired expiry callback to IHubContext<SprintHub> after app.MapHub<SprintHub>()
- `web/reflekt-web/src/stores/sprint.ts` — Added writingTimerDefault/votingTimerDefault/discussingTimerDefault to Sprint and SprintSummary interfaces; added three fields to CreateSprintPayload
- `web/reflekt-web/src/composables/useSprintHub.ts` — Imported timerStore and TimerPayload; registered TimerUpdated and TimerSnapshot event handlers
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` — Added sprintId + three timer default props; imported and rendered TimerDisplay during timed phases (Writing/Voting/Discussing); added isTimedPhase computed
- `web/reflekt-web/src/views/RetroRoomView.vue` — Passed sprintId and three timer defaults (from sprintStore.currentSummary) to RetroPhaseHeader
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — Added three timer default number inputs with labels; added refs (writingTimerDefault=5, votingTimerDefault=3, discussingTimerDefault=10); added validateTimerDefault() validation; included timer fields in emitted create payload

## Key Decisions

- **SM detection in hub:** Used IServiceScopeFactory to resolve scoped repos (ParticipantRepository, SprintRepository, TeamRepository) inside hub methods. SM is verified by checking participant.UserId == team.OwnerId. Guest-only participants (null UserId) are always rejected. No additional claims needed — avoids modifying the auth handler.
- **Timer expiry callback wiring:** Registered callback in UseReflektApi() after MapHub<SprintHub>() by resolving IHubContext<SprintHub> from app.Services. This avoids circular dependencies between TimerService (singleton) and the hub context.
- **Store vs composable split:** timerStore holds authoritative state (set by hub events). useTimer composable adds client-side countdown interval + audio/flash — only runs in component context where lifecycle hooks work. Module-level singleton composable was rejected as it breaks onUnmounted.
- **Audio chime:** Embedded as base64 WAV in composable to avoid external file dependency. Placeholder WAV — real audio asset can be dropped in to replace.
- **Two DB queries per timer hub call:** RequireScrumMasterAsync + LoadTimedSprintAsync each create a scope and load sprint. This is acceptable for timer operations (infrequent, not hot path). Could be merged if performance becomes a concern.

## Deviations from Plan

- **Step 8 timer store:** Plan says "delegates to useTimer composable." Implemented as a standalone options-API store (matching codebase pattern) that holds state directly. The useTimer composable watches the store. This is cleaner than having a module-level singleton composable, which breaks Vue lifecycle hooks.
- **Step 4 AddTime(Expired) behavior:** When expired and AddTime is called, timer transitions to Running state with a new countdown. This matches the plan's acceptance criteria: "AddTime expired → state becomes Running."
- **Pre-existing TypeScript errors:** TeamSelector.vue has 5 pre-existing TS errors unrelated to this feature (hasTeam vs hasTeams property name mismatch). Not introduced by this implementation.
