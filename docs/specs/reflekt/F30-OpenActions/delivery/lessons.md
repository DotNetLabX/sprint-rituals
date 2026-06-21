# Open Actions from Previous Retro — Lessons

## Developer Lessons

- **Options API stores:** The existing Pinia stores in this project use options API syntax (`defineStore('name', { state, actions, getters }`), not the composition API setup store pattern. The pinia-patterns skill defaults to composition API. Match the codebase pattern, not the skill pattern.
- **SprintState enum required for repository queries:** Filtering by `SprintState.Closed` in `GetMostRecentClosedByTeamIdAsync` required importing `Reflekt.Domain.Sprints` — the enum values are strongly typed, not string comparisons.
- **Duplicate import risk when adding imports to existing files:** When adding a new import to a component that already imports from the same module (e.g., `useSprintStore` was already imported in `RetroBoardPanel.vue`), check with Grep before adding to avoid duplicate import errors.
- **GetPreviousActionsEndpoint uses same auth pattern as ListCardsEndpoint:** The OwnerOrScrumMaster auth check needs to handle both participant (guest token) and owner (cookie) paths — the participant claim check vs. team OwnerId check is the standard pattern for sprint-scoped read endpoints.
- **EF migration from repo root requires src/ prefix:** Running `dotnet ef migrations add` requires `--project src/Services/...` and `--startup src/Services/...` prefixes when run from the repository root (not from within the solution folder).
- **SetReviewStatus no-op for same status is correct:** The spec says "Setting to the same status is a no-op (200)" — the domain method always sets and raises the event. This is technically not a true no-op at the domain level but simplifies the implementation and the spec allows it.

## Architect Lessons

- **Plan was well-scoped at 11 steps.** All 11 steps were implemented with zero deviations and no questions raised. The step granularity was correct -- each step was one focused task, and no step needed splitting or merging during implementation.
- **Shared contracts file was the right call.** Placing `PreviousActionsContracts.cs` at the feature folder root (shared by GET and PATCH) avoided duplication. This pattern should be the default when two endpoints in the same feature area share response types.
- **Report endpoint pattern for cross-sprint data requires two queries.** The BuildPreviousActionsSectionAsync pattern (query reviewed items by this sprint + query unreviewed eligible items from previous sprint) is the correct approach for "show everything eligible, mark what was acted on." This is a reusable pattern for any future cross-sprint feature.
- **AdvancePhaseCheck extension pattern is clean.** The existing endpoint's phase-branching structure (if Writing / if Voting / if Discussing) made adding the Writing branch straightforward. The pattern of returning null counts when no data exists (vs zero counts) is the right signal for the frontend to skip the dialog entirely.
- **Pre-commitment prediction on report logic was wrong -- good sign.** Predicted the developer might miss including unreviewed eligible items in the report. Developer implemented it correctly with both queries. The plan's acceptance criteria ("Items not reviewed by the SM appear as 'Not reviewed'") was specific enough to guide the implementation.
- **Options API stores vs Composition API:** The pinia-patterns skill documents composition API (setup stores), but this codebase uses options API exclusively. The developer correctly matched the codebase pattern. This is a known skill-vs-codebase divergence that should be documented.

## Skill Gaps

- **Missing skill:** `create-domain-event-handler` references `workflows/Handler.md` which does not exist in the skill directory. The skill's steps reference this file for location rules and naming conventions but it wasn't present. Used `BroadcastCardCreatedHandler.cs` as the reference pattern directly.

## Reviewer Lessons

- **Frontend build failures from prior features block build evidence.** The `TimerControls.vue` failure from F29 prevented a clean Vite build for F30. When a pre-existing build failure blocks full build verification, confirm via git log that the failing file was not touched by the current feature, document it clearly in the Evidence table, and use `vue-tsc --noEmit` filtered to feature files as a fallback.
- **`GetBySprintIdAsync` with EvidenceLinks eager load is a performance anti-pattern for count-only uses.** Two locations (AdvancePhaseCheck and BroadcastReviewStatusChangedHandler) use the general-purpose `GetBySprintIdAsync` that eagerly loads EvidenceLinks just to count/filter action items. Reviewers should flag any use of a broader query method when a narrower one exists — especially in hot paths like SignalR handlers.
- **Tooltip wiring from help.tooltips.md must be checked explicitly.** The `help-tooltips.md` rule requires reviewers to verify tooltip text matches the file. Easy to miss when the tooltip is on a dialog element (not a named component). Check every tooltip entry in the file against the rendered element, not just inline component tooltips.
- **Domain "no-op" language needs precise scoping.** When a spec says "Setting to the same status is a no-op (200)", clarify whether this means: (a) the HTTP response is 200, (b) no domain state changes, or (c) no SignalR event fires. All three are distinct. The current implementation satisfies (a) only. Future specs should be explicit about all three axes.
- **Fix cycle efficiency: targeted queries are a cheap one-method addition.** The `GetBySprintIdAsync` → `CountActionItemsBySprintIdAsync` fix required adding a single `CountAsync` repository method. When a MEDIUM finding involves loading a full graph just for a count, the fix cost is almost always this low — worth flagging at MEDIUM rather than LOW.
