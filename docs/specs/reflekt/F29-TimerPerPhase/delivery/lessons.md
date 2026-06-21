# Timer per Phase -- Lessons

## Architect Lessons
- The spec is at `docs/features/TimerPerPhase/spec.md` rather than the standard `docs/specs/{slug}/definition/spec.md` path. Feature specs live in a different location than the convention assumes. This is fine -- the plan references the actual path.
- No skill exists for adding hub methods to an existing SignalR hub. This is a recurring pattern (readiness was added the same way). A `add-hub-method` skill could codify: claim validation, group broadcast, snapshot-on-join pattern.
- No skill exists for ephemeral in-memory singleton services (like TimerService or the ReadinessState pattern in SprintHub). A `create-ephemeral-state-service` skill could cover: ConcurrentDictionary structure, thread safety patterns, DI registration as singleton, cleanup on dispose.
- The SprintHub already uses static ConcurrentDictionary fields for ReadinessState and ConnectionState. TimerService follows the same pattern but as a proper injected singleton service rather than static fields -- this is a better pattern. Consider refactoring ReadinessState out of SprintHub into a dedicated service in a future cleanup.
- Step 1 review observation: The plan said store "delegates to composable" but the developer correctly inverted this to "composable watches store" -- the plan's phrasing was ambiguous about the direction of delegation. For future plans involving store+composable splits, specify which is authoritative (store) vs reactive (composable) explicitly.
- Step 1 review observation: Leaving SM detection mechanism to developer judgment (plan said "developer decides") worked well -- the IServiceScopeFactory approach is the correct pattern for hubs needing scoped services. This validates the decision to not over-specify hub auth in the plan.
- Step 1 review observation: The plan mentioned GetSprintSummaryEndpoint explicitly but said "all GET sprint endpoints" for coverage. GetSprint uses Mapster auto-mapping so no explicit change was needed there -- but the plan should have been explicit about which endpoints need manual changes vs which auto-map. The developer handled this correctly via Mapster knowledge.

## Developer Lessons
- **Hub scoped service pattern:** SignalR hubs cannot inject scoped services directly (lifetime mismatch). Use IServiceScopeFactory injected in the hub constructor, then create a scope inside each hub method that needs scoped repos. This is the correct pattern for hubs that need EF repositories.
- **SM detection via OwnerId:** The guest token only carries participantId/sprintId/displayName. SM identity is verified by loading participant.UserId and matching against team.OwnerId. Participants who joined anonymously (null UserId) are automatically rejected. This avoids modifying the auth handler.
- **Singleton composable anti-pattern:** A useTimer composable created outside component setup (module-level singleton) will not have onUnmounted fire correctly, leaving intervals running. Correct pattern: state in Pinia store; composable used only inside component setup for lifecycle-bound side effects.
- **Expiry callback wiring:** TimerService (singleton) needs IHubContext<SprintHub> to broadcast on expiry. Register a callback in UseReflektApi() after MapHub() — IHubContext<SprintHub> is resolvable from app.Services at that point. Keeps TimerService infrastructure-agnostic.
- **Mapster auto-mapping:** Adding new properties to a domain entity with names matching positional record parameters in a response DTO is sufficient for Mapster auto-mapping. No explicit mapping config needed.

## Reviewer Lessons
- **Store getters that use snapshot state can silently mislead.** `timerStore.formattedTime` computes from `remainingSeconds` (updated only on hub events), not from the live interval. The store is the right source of truth for event state, but computed values that depend on wall-clock time should live only in lifecycle-bound composables. When reviewing Pinia stores with time-dependent computed values, verify whether the computation needs live ticking or is only valid at event-update time.
- **Pre-existing test compile failures mask test coverage gaps.** When a unit test project has pre-existing build errors, new tests won't be added — but the gap still exists. Reviewers should flag missing tests as MEDIUM even when pre-existing failures provide cover, and recommend fixing the test project in the same batch.
- **Confirm test file absence with Glob before flagging.** Using `Glob tests/**/*Timer*.cs` and checking `SprintTests.cs` content is the right verification step for test coverage gaps — don't rely on implementation.md to claim test coverage.
- **Production-only build is valid evidence when test project has pre-existing failures.** Build the main project separately (`dotnet build {Svc}.API.csproj`) to get clean evidence. Note the pre-existing unit test failures with their commit origin.

## Skill Gaps
- **Missing skill:** Hub method additions — no skill covers adding methods to an existing SignalR hub, SM validation, or scoped-service-in-hub lifetime management. Suggested name: `add-hub-method`. Reference file: `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`.
