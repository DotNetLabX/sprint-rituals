# Team Management — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs` — Added `Role` (string, default "Developer") and `DefaultCapacityPercent` (int, default 100) properties
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` — Added EF config for `Role` (HasMaxLength(64), HasDefaultValue("Developer")) and `DefaultCapacityPercent` (HasDefaultValue(100))
- `src/Services/Fokus/Fokus.Persistence/Migrations/*_AddDeveloperTeamConfig.cs` — Migration adding Role and DefaultCapacityPercent columns with defaults
- `src/Services/Fokus/Fokus.API/Features/Team/GetTeamRoster/GetTeamRosterEndpoint.cs` — GET /api/team endpoint returning TeamRosterResponse (developers + subTeams list)
- `src/Services/Fokus/Fokus.API/Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs` — PUT /api/developers/{accountId}/team-config endpoint with partial update semantics, validator enforcing role whitelist and capacity range
- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` — Static helper computing HashSet<string> of excluded developer IDs per sprint (0% effective capacity + 0 completed tickets)
- `client/src/api/team.ts` — getTeamRoster() and updateTeamConfig() API functions
- `client/src/stores/teamStore.ts` — Pinia setup store with fetchTeam(), updateConfig() (optimistic UI with revert on failure), groupedBySubTeam computed
- `client/src/views/TeamView.vue` — L0 page view: loading/error/empty/data states, mounts store fetch
- `client/src/components/team/TeamTable.vue` — L1 container: groups developers by sub-team, sorts alphabetically with inactive at bottom, "Unassigned" last
- `client/src/components/team/TeamRow.vue` — L3 row component: inline role select, capacity number input (amber when != 100), sub-team select + new sub-team text input, active checkbox; each field emits update on change

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — Removed `UpdateSubTeamAsync`, added `UpdateTeamConfigAsync` with partial update semantics and `subTeamProvided` bool parameter to distinguish null-clear from not-provided
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — `GetCapacity` now accepts `List<Developer> developers` and falls back to `developer.DefaultCapacityPercent`; `ComputeThroughput` and `ComputeRollingAverage` updated to accept and forward both `activeDevelopers` and `allDevelopers`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — Loads allDevelopers, computes excludedInAllTargets, filters activeDevelopers before passing to service
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — `FilterMemberships`, `ComputeFlags`, `BuildSparkline`, `ComputeSummary` all updated with optional `HashSet<string>? excludedDeveloperIds` parameter
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Loads allDevelopers and capacityRecords, computes excludedIds, passes to ComputeSummary
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — `FilterMemberships`, `ComputeMultiSprint`, `ComputeSingleSprint` updated with optional excludedDeveloperIds
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — Loads allDevelopers and capacityRecords, computes excluded IDs per mode, passes to service
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — `FilterMemberships`, `ComputeMultiSprint`, `ComputeSingleSprint` updated with optional excludedDeveloperIds
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` — Added DeveloperRepository injection, loads allDevelopers and capacityRecords, computes excluded IDs per mode
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — `FilterMemberships`, `ComputeMultiSprint`, `ComputeSingleSprint` updated with optional excludedDeveloperIds (4 internal call sites)
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` — Added DeveloperRepository injection, loads allDevelopers and capacityRecords, computes excluded IDs per mode
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — `FilterMemberships`, `ComputeSingleSprint`, `ComputeMultiSprint` updated with optional excludedDeveloperIds
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` — Added DeveloperRepository injection, loads allDevelopers and capacityRecords, computes excluded IDs per mode
- `client/src/types/index.ts` — Added TeamDeveloperDto, TeamRosterResponse, UpdateTeamConfigRequest interfaces
- `client/src/router.ts` — Added /team route (lazy-import TeamView.vue)
- `client/src/components/AppSidebar.vue` — Added "Team" nav item between Cycle Time and Settings
- `client/src/api/developers.ts` — Removed setDeveloperSubTeam function
- `client/src/views/SettingsView.vue` — Removed Sub-Team Management section (template), removed all sub-team state variables and functions, removed getDevelopers/setDeveloperSubTeam imports, removed Developer type import, removed getDevelopers() from Promise.allSettled

## Files Deleted

- `src/Services/Fokus/Fokus.API/Features/Developers/SetSubTeam/SetSubTeamEndpoint.cs` — Replaced by UpdateTeamConfig endpoint

## Key Decisions

- **subTeamProvided pattern**: `UpdateTeamConfigRequest` arrives as JSON; used `JsonDocument` to detect presence of the `subTeam` key (checks both "subTeam" and "SubTeam" casing). This allows `null` to mean "clear" vs. key absent meaning "don't change". A `bool subTeamProvided` parameter threads through to `UpdateTeamConfigAsync`.
- **Multi-sprint exclusion**: A developer is only excluded from multi-sprint analytics if they are excluded in ALL selected sprints. Computed with `GroupBy().Where(count == sprints.Count)`.
- **Service signature extension**: All analytics services gained optional `HashSet<string>? excludedDeveloperIds = null` on `FilterMemberships`, `ComputeSingleSprint`, and `ComputeMultiSprint` — backward compatible, existing callers unaffected.
- **allDevelopers vs activeDevelopers**: Exclusion filter requires all developers (not just active) to look up DefaultCapacityPercent for capacity fallback. Endpoints load `allDevelopers` once for fallback + exclusion, but filter to `activeDevelopers` before analytics computation.
- **teamStore optimistic revert**: On API failure, the store reverts to a saved `previous` snapshot and re-throws so the view can handle UI feedback.

## Review Cycle 1 Fixes

- `client/src/stores/teamStore.ts` — After successful API response, `updateConfig` now reconciles the `subTeams` list: adds the new sub-team (sorted) if not already present, removes the old sub-team if no other developer still uses it.
- `client/src/components/AppSidebar.vue` — Moved "Team" nav entry to immediately after "Developers" (was after "Cycle Time"). Order is now: Dashboard, Developers, Team, Sprints, Epics, Cycle Time, Settings.

## Deviations from Plan

- The `UpdateTeamConfigRequest` TypeScript type was defined with `string | null` on `role` and `number | null` on `defaultCapacityPercent` to accommodate optional clearing semantics cleanly, whereas the plan spec uses plain optional `?`. This is compatible — null is simply not sent by the UI for those fields.
