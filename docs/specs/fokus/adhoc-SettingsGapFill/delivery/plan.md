# Settings Gap Fill (GAP-1 + GAP-2 + GAP-3)

**Feature Spec:** None

## Context
Three UI gaps in the Settings page where backend functionality exists but no frontend was built. Addressed together because all three land on `SettingsView.vue` — one pass avoids merge friction.

- **GAP-1** (High): Excluded-from-scope statuses UI. Endpoints exist (`GET/PUT /api/settings/excluded-statuses`), frontend API functions exist (`getExcludedStatuses`, `saveExcludedStatuses`), but no Settings section.
- **GAP-2** (Medium): Sprint range picker for sync. `POST /api/sync/sprints` accepts `{ fromSprintId, toSprintId }` but the UI has no dropdowns — only a "Sync All" button that hardcodes `.slice(-5)`. Users need from/to dropdowns for custom range AND a configurable "sync back N sprints" setting for Sync All (default 20).
- **GAP-3** (Medium): Sub-team management UI. `Developer.SubTeam` exists, `GET /api/developers/sub-teams` returns distinct values, analytics pages have sub-team filters — but no UI to assign sub-teams to developers. No update endpoint exists either.

## Scope

### In scope
- GAP-1: Excluded statuses section in Settings (frontend only — backend + API functions already exist)
- GAP-2: `SyncBackSprintCount` setting on AppSettings (backend + frontend) + from/to sprint dropdowns in sync section (frontend — backend already accepts the range)
- GAP-3: `PUT /api/developers/{accountId}/sub-team` endpoint (backend) + developer sub-team assignment section in Settings (frontend) + `GET /api/developers` list endpoint (backend — needed to show all developers)

### Out of scope
- Adding `SprintState.Future` to `GetJiraSprintsEndpoint` or `SyncSprintsEndpoint` — future sprints are already handled by the existing `POST /api/sync/backlog` endpoint which calls `GetSprintsAsync(boardId, ct, SprintState.Future)`. Sync All already calls both `syncSprints()` and `syncBacklog()`.
- Changes to analytics pages or sub-team filter behavior

## Domain Model Changes
- `AppSettings`: add `int SyncBackSprintCount` (default 20)
- No new aggregates or domain events

## Data Model Changes
- `AppSettings` table: new column `SyncBackSprintCount` (int, default 20). Requires EF migration.

## Implementation Steps

### GAP-2 Backend: SyncBackSprintCount setting

**Step 1: Add SyncBackSprintCount to AppSettings**
- Add `public int SyncBackSprintCount { get; set; } = 20;` to `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`
- Add same default in `CreateDefault()`
- Add property to `GetSettingsResponse` in `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs`
- Map it in `GetSettingsEndpoint.cs`
- Add property to `SaveSettingsCommand` in `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs`
- Add validation: `InclusiveBetween(1, 50)` in `SaveSettingsCommandValidator`
- Map it in `SaveSettingsEndpoint.cs`
- No EF configuration change needed — it's a simple int, convention handles it

**Step 2: EF migration**
- Run: `dotnet ef migrations add AddSyncBackSprintCount -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API`

### GAP-3 Backend: Developer list + sub-team assignment endpoint

**Step 3: Add GET /api/developers endpoint**
- Create `src/Services/Fokus/Fokus.API/Features/Developers/GetDevelopers/GetDevelopersEndpoint.cs`
- Returns list of `{ accountId, displayName, avatarUrl, subTeam, isActive }` using `DeveloperRepository.GetAllAsync()`
- Follow pattern of `GetSubTeamsEndpoint.cs`

**Step 4: Add PUT /api/developers/{accountId}/sub-team endpoint**
- Create `src/Services/Fokus/Fokus.API/Features/Developers/SetSubTeam/SetSubTeamEndpoint.cs`
- Request: `{ subTeam: string | null }` (null clears the sub-team)
- Loads developer by `accountId`, sets `SubTeam`, saves via repository
- Returns updated developer DTO
- Follow pattern of `SetCapacityEndpoint.cs`
- Add `UpdateSubTeamAsync(string accountId, string? subTeam)` to `DeveloperRepository` — loads developer, sets `SubTeam`, calls `SaveChangesAsync`

### GAP-2 + GAP-1 + GAP-3 Frontend

**Step 5: Add frontend types and API functions**
- `client/src/types/index.ts`:
  - Add `syncBackSprintCount: number` to `AppSettings` interface
  - Add `'Future'` to `SprintState` type
  - Add `Developer` interface: `{ accountId: string, displayName: string, avatarUrl: string | null, subTeam: string | null, isActive: boolean }`
- `client/src/api/developers.ts`:
  - Add `getDevelopers(): Promise<Developer[]>` → `GET /api/developers`
  - Add `setDeveloperSubTeam(accountId: string, subTeam: string | null): Promise<Developer>` → `PUT /api/developers/{accountId}/sub-team`

**Step 6: SettingsView — GAP-1: Excluded Statuses section**
- Add section after "Done Statuses" (same visual pattern)
- On mount: call `getExcludedStatuses()` in the existing `Promise.allSettled` batch
- UI: status chips with × remove + dropdown to add (reuse `statuses` data already loaded, filter out already-excluded)
- Own save button (like Cycle Time Boundaries) — calls `saveExcludedStatuses()`
- Follow the exact chip + dropdown pattern from Done Statuses section

**Step 7: SettingsView — GAP-2: Sync section rewrite**
- Add `syncBackSprintCount` to the reactive `form` object (default 20), wire to settings store
- Add number input for "Sync back N sprints" in the Sync section, label: "Default sprint count"
- This is saved with the main "Save Settings" button (part of `form`)
- Rewrite `syncAll()`: replace `.slice(-5)` with `.slice(-form.syncBackSprintCount)`
- Add from/to sprint dropdowns below the Sync All button:
  - Load sprint list from `getJiraSprints(boardId)` on demand (when user opens sync section or clicks a "load sprints" trigger)
  - Two `<select>` dropdowns: "From sprint" and "To sprint", populated with all returned sprints (ordered by startDate)
  - "Sync Range" button calls `syncSprints(fromId, toId)`
  - Show same result summary as Sync All
  - Backlog sync checkbox or note: "Sync All also syncs future sprints and epics"

**Step 8: SettingsView — GAP-3: Sub-Team Management section**
- Add section after Sync section (or after Bug Ratio Alerts, before Save)
- On mount: call `getDevelopers()` in the `Promise.allSettled` batch
- UI: table/list of developers with columns: avatar, name, sub-team input/dropdown, save button per row
  - Sub-team input: free-text `<input>` (not a dropdown — sub-teams are created by typing them, no predefined list)
  - Clear button (×) to remove sub-team assignment
  - Inline save per developer — calls `setDeveloperSubTeam(accountId, subTeam)`
  - Show success/error feedback per row
- Only show active developers (filter `isActive`)

## Migration Notes
```bash
dotnet ef migrations add AddSyncBackSprintCount -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

## Testing Strategy
- **GAP-1**: Add excluded statuses, save, refresh page — verify they persist. Verify scope change metrics now show different Active vs Total committed SP.
- **GAP-2**: Change sync-back count to 3, Sync All — verify only 3 sprints synced. Use from/to to sync a single sprint (same ID in both). Verify Sync All still syncs backlog (future sprints + epics).
- **GAP-3**: Assign sub-team to a developer, verify it persists on refresh. Clear sub-team. Verify analytics sub-team filter now shows the assigned team.

## Open Questions
None — all three gaps have clear backend contracts and UI patterns to follow.
