# Team Management

**Feature Spec:** `docs/features/TeamManagement/spec.md`

## Context

Scrum Masters need a dedicated place to configure their team roster — roles, default capacity, sub-team assignment, and active status. Today, developers appear from Jira sync with no way to set default capacity or roles. Sub-team assignment exists in Settings but belongs alongside the rest of the team configuration. People who don't contribute SP (POs, testers, TLs with reduced capacity) clutter every analytics view.

This feature adds a "Team" page to the sidebar, creates a new `PUT /api/developers/{accountId}/team-config` endpoint, adds a `GET /api/team` endpoint, extends the Developer entity with `Role` and `DefaultCapacityPercent`, implements a cross-cutting exclusion rule (0% effective capacity + 0 completed tickets = hidden from analytics), replaces the hardcoded 100% capacity fallback with lazy resolution from `Developer.DefaultCapacityPercent`, and removes the existing sub-team management section from Settings along with its `PUT /api/developers/{accountId}/sub-team` endpoint.

**Services impacted:** Fokus (single service). Backend extends the Developer entity, adds two endpoints (GET team roster, PUT team-config), removes one endpoint (PUT sub-team), modifies one analytics service (DeveloperThroughputService lazy fallback) and adds a cross-cutting exclusion filter. Frontend adds a new Team page with store, API module, types, and components, and removes the sub-team section from SettingsView.

## Scope

**In scope:**
- Two new properties on Developer entity: `Role` (string, default "Developer"), `DefaultCapacityPercent` (int, default 100)
- EF Core migration for the new columns
- `GET /api/team` endpoint returning all developers with team config fields + distinct sub-team list
- `PUT /api/developers/{accountId}/team-config` endpoint with partial update semantics
- Removal of `PUT /api/developers/{accountId}/sub-team` endpoint and its feature folder
- Lazy capacity fallback: `DeveloperThroughputService.GetCapacity` falls back to `Developer.DefaultCapacityPercent` instead of hardcoded 100
- Cross-cutting exclusion rule: developers with 0% effective capacity AND 0 completed tickets are excluded from sprint analytics
- Frontend: Team page with grouped table, inline editing, optimistic UI
- Sidebar navigation entry for "Team" (after "Developers")
- Route `/team`
- Removal of sub-team management section from SettingsView.vue
- Removal of `setDeveloperSubTeam` from frontend API module

**Explicitly out of scope:**
- Manual developer add/remove
- Custom role labels beyond the fixed four
- Role-based default capacity auto-setting
- Sub-team CRUD as first-class entities
- Capacity history/audit log
- Bulk editing
- Sprint-level capacity override on this page
- Snapshot at sync (copying default capacity into DeveloperSprintCapacity at sync time)
- Real-time updates (no SignalR)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | Two new columns on Developer entity, EF config update, migration | |
| 2 | create-feature | Follow | FastEndpoints GET `/api/team`, `EndpointWithoutRequest<TeamRosterResponse>` | |
| 3 | create-feature | Follow | FastEndpoints PUT `/api/developers/{accountId}/team-config`, `Endpoint<UpdateTeamConfigRequest, TeamConfigResponse>` with Validator | |
| 4 | (none) | -- | Remove SetSubTeam endpoint folder and repository method | Inline: deletion, no skill needed |
| 5 | (none) | -- | Modify `DeveloperThroughputService.GetCapacity` to accept developers list and use `DefaultCapacityPercent` fallback | Inline: modifies existing service method signature |
| 6 | (none) | -- | Add cross-cutting exclusion filtering in analytics endpoints that load active developers | Inline: cross-cutting filter logic across multiple endpoints |
| 7 | create-vue-feature | Follow | Types, API module (`client/src/api/team.ts`), route, sidebar entry | |
| 8 | pinia-patterns | Follow | `teamStore` with fetch, optimistic update actions | |
| 9 | vue-component-architecture | Follow | `TeamView.vue` L0 page, `TeamTable.vue` L1 container, `TeamRow.vue` L3 row component | |
| 10 | (none) | -- | Remove sub-team management section from SettingsView.vue and cleanup imports | Inline: deletion from existing view |

## Domain Model Changes

**Modified entity: Developer**
- Add `Role` property: `string`, default `"Developer"`. Informational label, no business logic.
- Add `DefaultCapacityPercent` property: `int`, default `100`. Used as fallback when no `DeveloperSprintCapacity` record exists.

**No new aggregates, entities, or value objects.** The Developer entity is not an aggregate (Entity<string>), and these are simple scalar properties added to it.

## Data Model Changes

**Modified table:** `Developers`
- Add column `Role` (`TEXT`, not null, default `'Developer'`, max length 64)
- Add column `DefaultCapacityPercent` (`INTEGER`, not null, default `100`)

**No new tables.**

## Implementation Steps

### Step 1: Extend Developer entity and persist new columns

**What:** Add `Role` and `DefaultCapacityPercent` properties to the Developer entity. Update the EF configuration. Add a migration.

**Follow:** `persistence-patterns`

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs` -- add `Role` (string, default "Developer") and `DefaultCapacityPercent` (int, default 100)
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` -- configure `Role` with `HasMaxLength(64).HasDefaultValue("Developer")` and `DefaultCapacityPercent` with `HasDefaultValue(100)`

**Files to create:**
- `src/Services/Fokus/Fokus.Persistence/Migrations/{timestamp}_AddDeveloperTeamConfig.cs` -- via `dotnet ef migrations add`

**Dependencies:** None.

### Step 2: Create GET /api/team endpoint

**What:** Create a new endpoint that returns the full team roster with all team config fields, plus a distinct list of sub-team names for dropdown population. This replaces the separate `GET /api/developers` + `GET /api/developers/sub-teams` calls for the Team page.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Team/GetTeamRoster/GetTeamRosterEndpoint.cs` -- `EndpointWithoutRequest<TeamRosterResponse>`. Route: `GET /api/team`. Tags: `"Team"`.

**Response shape (`TeamRosterResponse`):**
```
TeamRosterResponse:
  developers: List<TeamDeveloperDto>
  subTeams: List<string>

TeamDeveloperDto:
  accountId: string
  displayName: string
  avatarUrl: string?
  role: string
  defaultCapacityPercent: int
  subTeam: string?
  isActive: bool
```

**Implementation:** Query `DeveloperRepository.GetAllAsync()` for developers. Query `DeveloperRepository.GetDistinctSubTeamsAsync()` for sub-teams. Map to DTOs.

**Dependencies:** Step 1.

### Step 3: Create PUT /api/developers/{accountId}/team-config endpoint

**What:** Create a new endpoint that updates any combination of team config fields on a developer. All fields are optional -- omitted fields are not changed. Returns the updated developer entry. Includes a `Validator<UpdateTeamConfigRequest>` for validation rules.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs` -- `Endpoint<UpdateTeamConfigRequest, TeamConfigResponse>`. Route: `PUT /api/developers/{accountId}/team-config`. Tags: `"Team"`.

**Request shape (`UpdateTeamConfigRequest`):**
```
UpdateTeamConfigRequest:
  accountId: string (from route)
  role: string? (optional, must not be empty/whitespace if provided)
  defaultCapacityPercent: int? (optional, 0-100)
  subTeam: string? (optional, nullable -- null clears)
  isActive: bool? (optional)
```

**Response shape (`TeamConfigResponse`):** Same as `TeamDeveloperDto` from Step 2.

**Validation rules (Validator sibling class):**
- `defaultCapacityPercent` when present: `InclusiveBetween(0, 100)`
- `role` when present: `NotEmpty()`, must be one of: "Developer", "Tech Lead", "Tester", "PO"

**Repository method to add:** `DeveloperRepository.UpdateTeamConfigAsync(string accountId, string? role, int? defaultCapacityPercent, string? subTeam, bool? isActive)` -- loads developer by ID, applies only non-null fields, returns updated entity or null if not found. Pattern: follow `UpdateSubTeamAsync` (line 84-90 in DeveloperRepository.cs).

**Error responses:** 404 if developer not found. 400 for validation failures.

**Dependencies:** Step 1.

### Step 4: Remove SetSubTeam endpoint and clean up

**What:** Delete the `SetSubTeam` feature folder and remove the `UpdateSubTeamAsync` method from `DeveloperRepository`. The `PUT /api/developers/{accountId}/team-config` endpoint (Step 3) now handles sub-team assignment via the `subTeam` field.

**No skill -- deletion.**

**Files to delete:**
- `src/Services/Fokus/Fokus.API/Features/Developers/SetSubTeam/SetSubTeamEndpoint.cs` (entire file)

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` -- remove `UpdateSubTeamAsync` method (lines 84-90)

**Dependencies:** Step 3 (replacement must exist before removal).

### Step 5: Implement lazy capacity fallback

**What:** Modify `DeveloperThroughputService.GetCapacity` to accept the developers list and fall back to `Developer.DefaultCapacityPercent` instead of hardcoded `100`. This change also requires the `GetDeveloperThroughputEndpoint` to pass the developers list to the service.

**No skill -- modifies existing service method signature.**

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`:
  - Change `ComputeThroughput` signature to accept `List<Developer> allDevelopers` (rename from `activeDevelopers` for clarity that this list now includes the full set for fallback lookup)
  - Change `GetCapacity` static method signature: add `List<Developer> developers` parameter
  - In `GetCapacity`: when no capacity record found, look up developer by ID in the developers list and return `developer.DefaultCapacityPercent` instead of `return 100`
  - Update all call sites of `GetCapacity` within the service to pass the developers list
  - Update `ComputeRollingAverage` to also accept and forward the developers list to `GetCapacity`

**Pattern reference:** Current `GetCapacity` at line 223-234 of `DeveloperThroughputService.cs`.

**Dependencies:** Step 1 (Developer entity must have DefaultCapacityPercent).

### Step 6: Implement cross-cutting exclusion rule in analytics

**What:** When a developer has 0% effective capacity for a sprint AND completed 0 tickets in that sprint, exclude their tickets from all sprint-level analytics. This is a cross-cutting filter applied at the endpoint level before passing data to analytics services.

The exclusion works by filtering out SprintMemberships belonging to excluded developers. Each analytics endpoint that loads active developers needs to compute "excluded developer IDs for this sprint" and filter memberships accordingly.

**Implementation approach:** Create a static helper method `ExcludedDeveloperFilter.GetExcludedDeveloperIds(Sprint sprint, List<Developer> developers, List<DeveloperSprintCapacity> capacityRecords, List<string> doneStatuses)` that returns `HashSet<string>` of developer account IDs to exclude. Place it in `Features/Analytics/ExcludedDeveloperFilter.cs`.

The filter logic:
1. For each developer, compute effective capacity: check `DeveloperSprintCapacity` for this sprint, else use `Developer.DefaultCapacityPercent`
2. If effective capacity > 0, developer is NOT excluded (regardless of completions)
3. If effective capacity == 0, check if developer completed any ticket (any membership with `AssigneeId == developerId`, `RemovedAt == null`, `FinalStatus in doneStatuses`) -- if yes, NOT excluded; if no, excluded

**No skill -- cross-cutting filter logic.**

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` -- static class with `GetExcludedDeveloperIds` method

**Files to modify -- endpoints that need the exclusion filter:**

Note: The exclusion filter removes excluded developers from the developer list AND filters out their tickets from sprint memberships. Each endpoint applies this differently based on what it passes to its service.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` -- load capacity records (already done), compute excluded IDs per target sprint, filter `activeDevelopers` list to remove excluded devs. The service already filters memberships by developer, so removing excluded devs from the list is sufficient.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` -- load capacity records for window sprint IDs, compute excluded IDs for the selected sprint, filter `activeDevelopers` and filter sprint memberships (removing tickets assigned to excluded developers). The SprintSummaryService receives memberships directly and uses the developer list for the leaderboard.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` -- load capacity records, compute excluded IDs per sprint, filter `activeDevelopers` list. The BugRatioService filters memberships by developer internally.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` -- load developers + capacity records (new dependency), compute excluded IDs per sprint, filter sprint memberships before passing to ScopeChangeService. This endpoint currently does not load developers, so DeveloperRepository must be injected.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` -- same pattern as ScopeChange: add DeveloperRepository + capacity loading, filter memberships.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` -- same pattern: add DeveloperRepository + capacity loading, filter memberships.

**Performance approach:** Load capacity records in bulk for all sprint IDs being processed (using existing `GetCapacitiesForSprintsAsync`). Load all developers once (using `GetAllAsync`, not just active -- because exclusion applies to all developers, not just active ones). The filter runs per sprint, but data loading is bulk.

**Dependencies:** Steps 1, 5.

### Step 7: Add frontend types and API module

**What:** Define TypeScript types for the Team API responses and create the API module. Add the route and sidebar entry.

**Follow:** `create-vue-feature`

**Files to modify:**
- `client/src/types/index.ts` -- add `TeamDeveloperDto`, `TeamRosterResponse`, `UpdateTeamConfigRequest`
- `client/src/router.ts` -- add `/team` route (after `/developers`, before `/sprints`), lazy-import `TeamView.vue`
- `client/src/components/AppSidebar.vue` -- add "Team" nav item after "Developers" with a people/group icon

**Files to create:**
- `client/src/api/team.ts` -- `getTeamRoster(): Promise<TeamRosterResponse>` and `updateTeamConfig(accountId: string, config: UpdateTeamConfigRequest): Promise<TeamDeveloperDto>`

**TypeScript types:**
```typescript
interface TeamDeveloperDto {
  accountId: string
  displayName: string
  avatarUrl: string | null
  role: string
  defaultCapacityPercent: number
  subTeam: string | null
  isActive: boolean
}

interface TeamRosterResponse {
  developers: TeamDeveloperDto[]
  subTeams: string[]
}

interface UpdateTeamConfigRequest {
  role?: string
  defaultCapacityPercent?: number
  subTeam?: string | null
  isActive?: boolean
}
```

**Dependencies:** Steps 2, 3.

### Step 8: Create Team Pinia store

**What:** Create a Pinia setup store for the Team page. Manages developer list, sub-team list, loading/error state, and optimistic update actions.

**Follow:** `pinia-patterns`

**Files to create:**
- `client/src/stores/teamStore.ts`

**Store shape:**
- **State:** `developers: TeamDeveloperDto[]`, `subTeams: string[]`, `loading: boolean`, `error: string | null`
- **Computed:** `groupedBySubTeam` -- groups developers by sub-team, sorts each group alphabetically by displayName with inactive at bottom, "Unassigned" group last
- **Actions:**
  - `fetchTeam()` -- calls `getTeamRoster()`, populates state
  - `updateConfig(accountId, config)` -- optimistic: apply changes to local state immediately, call `updateTeamConfig()`, on error revert. On success, if sub-team changed, update the `subTeams` list (add new sub-team if typed, remove old if no one else uses it)

**Dependencies:** Step 7.

### Step 9: Build Team page components

**What:** Create the Team view and its child components. The page is a table grouped by sub-team with inline editing.

**Follow:** `vue-component-architecture`

**Files to create:**
- `client/src/views/TeamView.vue` -- L0 page. Calls `teamStore.fetchTeam()` on mount. Three states: loading, error, data. Renders page title + `TeamTable`.
- `client/src/components/team/TeamTable.vue` -- L1 container. Receives `groupedBySubTeam` from store. Renders sub-team group headers and `TeamRow` for each developer. Handles the grouping layout (sub-team name as section header, "Unassigned" at bottom).
- `client/src/components/team/TeamRow.vue` -- L3 props-driven row. Receives a `TeamDeveloperDto` + `subTeams: string[]` as props. Emits `update(accountId, config)`. Contains:
  - Avatar (read-only) + display name (read-only)
  - Role: inline dropdown (`<select>`) with options: Developer, Tech Lead, Tester, PO
  - Default capacity: inline number input (0-100) with visual indicator when != 100 (e.g., amber text color or badge)
  - Sub-team: inline combobox (existing `<select>` with options from `subTeams` + a text input for new names, or a combined combobox component). Supports clearing (set to unassigned).
  - Active toggle: `<input type="checkbox">` or toggle switch
  - Each field saves immediately on change (emit `update` which the parent routes to `teamStore.updateConfig`)
  - Inactive rows: greyed out styling (opacity or muted text colors)

**Pattern reference:** Follow `client/src/views/DevelopersView.vue` for table layout conventions and `client/src/views/SettingsView.vue` for inline editing patterns.

**Dependencies:** Step 8.

### Step 10: Remove sub-team management from Settings and clean up frontend

**What:** Remove the "Sub-Team Management" section from SettingsView.vue. Remove the `setDeveloperSubTeam` function from the developers API module. Remove any imports and state variables related to sub-team management in SettingsView.

**No skill -- deletion from existing view.**

**Files to modify:**
- `client/src/views/SettingsView.vue`:
  - Remove the `<!-- Sub-Team Management (GAP-3) -->` section (lines 991-1032)
  - Remove `setDeveloperSubTeam` from the import on line 6
  - Remove `getDevelopers` from the import on line 6 (if no longer needed after removing sub-team section -- check: developers were loaded only for sub-team management)
  - Remove all sub-team state variables: `developers`, `developerSubTeamInputs`, `developerSaving`, `developerSaved`, `developerError` (lines 143-148)
  - Remove functions: `saveSubTeam`, `clearSubTeam` (lines 149-172)
  - Remove `getDevelopers()` from the `Promise.allSettled` call in `onMounted` (line 216) and its result handling (lines 252-257)
  - Remove `Developer` from the type import if no longer used
- `client/src/api/developers.ts`:
  - Remove the `setDeveloperSubTeam` function (lines 8-13)

**Dependencies:** Step 9 (Team page must exist as the replacement).

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

```bash
# Add migration (from repo root)
dotnet ef migrations add AddDeveloperTeamConfig -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API

# Apply migration
dotnet ef database update -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

The migration adds two columns to the existing `Developers` table with default values, so existing rows will be backfilled:
- `Role = 'Developer'`
- `DefaultCapacityPercent = 100`

No data loss. No seed data needed.

## Testing Strategy

**Backend:**
- `GET /api/team` returns all developers with role, defaultCapacityPercent, subTeam, isActive fields
- `GET /api/team` response includes distinct sub-team list
- `PUT /api/developers/{id}/team-config` with `role: "Tech Lead"` updates only role, other fields unchanged
- `PUT /api/developers/{id}/team-config` with `defaultCapacityPercent: 50` updates only capacity
- `PUT /api/developers/{id}/team-config` with `subTeam: null` clears sub-team
- `PUT /api/developers/{id}/team-config` with `defaultCapacityPercent: 150` returns 400
- `PUT /api/developers/{id}/team-config` with `role: ""` returns 400
- `PUT /api/developers/{id}/team-config` with `role: "Manager"` returns 400 (not in allowed list)
- `PUT /api/developers/{nonexistent}/team-config` returns 404
- Old `PUT /api/developers/{id}/sub-team` returns 404 (endpoint removed)
- Lazy fallback: developer with `DefaultCapacityPercent=60` and no DeveloperSprintCapacity record shows 60% capacity in developer throughput
- Exclusion: developer with 0% effective capacity and 0 completed tickets is absent from sprint summary leaderboard, flags, and metric computations
- Exclusion: developer with 0% capacity but 1 completed ticket appears normally
- Exclusion per-sprint: same developer excluded in one sprint, visible in another

**Frontend:**
- Team page loads and displays grouped table
- Inline role change saves optimistically
- Inline capacity change saves optimistically, visual indicator appears for non-100 values
- Sub-team change moves row to correct group
- Active toggle greys out row
- Sub-team management section is gone from Settings page
- Navigating to `/team` works, sidebar highlights correctly

## Open Questions

None.
