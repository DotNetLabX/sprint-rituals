# Settings Gap Fill — Implementation

## Files Created
- `src/Services/Fokus/Fokus.API/Features/Developers/GetDevelopers/GetDevelopersEndpoint.cs` — GET /api/developers endpoint returning all developers as DeveloperDto list
- `src/Services/Fokus/Fokus.API/Features/Developers/SetSubTeam/SetSubTeamEndpoint.cs` — PUT /api/developers/{accountId}/sub-team endpoint with request/response/no separate validator (no additional validation needed beyond 404 check)
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260509*.cs` — EF migration AddSyncBackSprintCount (auto-generated)

## Files Modified
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added `SyncBackSprintCount` property (default 20) and same default in `CreateDefault()`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Added `SyncBackSprintCount` to `GetSettingsResponse`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Mapped `SyncBackSprintCount` in response construction
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — Added `SyncBackSprintCount` property (default 20) and `InclusiveBetween(1, 50)` validation rule
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — Mapped `SyncBackSprintCount` in AppSettings construction
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — Added `UpdateSubTeamAsync(accountId, subTeam, ct)` method
- `client/src/types/index.ts` — Added `syncBackSprintCount: number` to `AppSettings`, added `'Future'` to `SprintState`, added `Developer` interface
- `client/src/api/developers.ts` — Added `getDevelopers()` and `setDeveloperSubTeam()` API functions
- `client/src/stores/settingsStore.ts` — Added `syncBackSprintCount: 20` to default settings object
- `client/src/views/SettingsView.vue` — Full three-gap implementation: imports, state, computed, handlers, template sections

## Key Decisions
- `SetSubTeamEndpoint` has no separate validator class — the only validation is 404 if developer not found. Null subTeam is valid (clears assignment). No extra FluentValidation needed.
- `GetDevelopersEndpoint` returns all developers (not just active) from repository; the frontend filters to `isActive` as specified by the plan, keeping the endpoint general-purpose.
- `UpdateSubTeamAsync` in repository loads the developer, sets SubTeam, returns the entity. The endpoint calls `SaveChangesAsync` separately — following the existing pattern from `SetCapacityEndpoint`.
- Frontend sync range sprint list is loaded on demand via "Load Sprints" button rather than on mount, to avoid an extra API call for users who don't need custom range.
- `selectedExcludedStatus` default is set after statuses load (in the same block as `selectedStatus`) to keep the logic consolidated.

## Deviations from Plan
- None. All 8 plan steps implemented as specified.

## Fixes Applied (Cycle 1)
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260509192107_AddSyncBackSprintCount.cs` — Changed `defaultValue: 0` to `defaultValue: 20` so existing rows get the correct default when the migration runs.
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — Added `GetAsync` call before constructing the new `AppSettings` object; preserves `ExcludedFromScopeStatuses` from the existing record instead of overwriting it with `[]`.
