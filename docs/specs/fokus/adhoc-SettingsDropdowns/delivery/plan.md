# Settings Dropdowns

**Feature Spec:** None

## Context

The Settings page currently uses a numeric input for Board ID and a free-text input for Done Statuses. Both should be replaced with dropdowns populated from Jira, improving UX and reducing configuration errors. The Board selector backend already exists (`GET /api/boards`); only the frontend needs changing. The Done Statuses dropdown requires a new backend endpoint that fetches statuses from the Jira REST API.

## Scope

**In scope:**
- Board selector: replace `<input type="number">` with a `<select>` dropdown that calls `GET /api/boards`
- Done Statuses: new Jira module method, new API endpoint, replace free-text input with a multi-select dropdown
- Graceful fallback: when Jira is unreachable or returns an error, fall back to the current manual input UI for both fields

**Out of scope:**
- Workflow Stages dropdown (separate feature if desired)
- Caching of Jira board/status lists
- Project-specific status filtering (uses `GET /rest/api/3/status` for all statuses)

## Domain Model Changes

None. This is pure API plumbing and UI work. No aggregates, entities, or domain events change.

## Data Model Changes

None. No new tables, columns, or migrations.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 | None | Jira module contract addition -- follows existing `GetBoardsAsync` pattern in `IJiraClient.cs` |
| 2 | None | Refit interface addition -- follows existing `GetBoardsPageAsync` pattern in `IJiraApi.cs` |
| 3 | None | Client implementation -- follows existing `GetBoardsAsync` pattern in `RestApiJiraClient.cs` |
| 4 | `create-feature` | New FastEndpoints endpoint following GetBoards pattern |
| 5 | None | Frontend API + types -- follows existing `client/src/api/settings.ts` pattern |
| 6 | None | Vue component update -- modifies existing `SettingsView.vue` |

## Implementation Steps

### Step 1: Add JiraStatus DTO to Jira.Contracts

**What:** Add a `JiraStatus` record to hold the status data returned by Jira's REST API.

**Files to create:**
- `src/Modules/Jira/Jira.Contracts/JiraStatus.cs`

**Pattern:** Follow `src/Modules/Jira/Jira.Contracts/JiraBoard.cs` -- simple DTO class with public properties.

**Details:** The Jira `GET /rest/api/3/status` response returns an array of objects with at minimum `id` (string), `name` (string), `statusCategory.key` (string). The DTO needs `Id` (string), `Name` (string), and `CategoryKey` (string). The category key is useful for the frontend to visually distinguish done-like statuses (category key `"done"`) from others.

### Step 2: Add GetStatusesAsync to IJiraClient

**What:** Add a `GetStatusesAsync` method to the Jira client contract.

**Files to modify:**
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` -- add `Task<List<JiraStatus>> GetStatusesAsync(CancellationToken ct);`

**Pattern:** Follow the existing `GetBoardsAsync` signature.

### Step 3: Add Refit method and implement in RestApiJiraClient

**What:** Add the Refit interface method for `GET /rest/api/3/status` and implement the client method.

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` -- add Refit method
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` -- implement `GetStatusesAsync`

**Pattern:** Follow the existing board methods in both files.

**Details:**
- The `/rest/api/3/status` endpoint returns a **flat JSON array** (not a paged result). The Refit method signature should be `Task<IApiResponse<List<JiraStatus>>> GetStatusesAsync(CancellationToken ct)`.
- In `RestApiJiraClient`, call `RequestAsync(() => api.GetStatusesAsync(ct), ct)` directly -- no pagination loop needed.
- Deduplicate by status name before returning (Jira can return the same status name across multiple projects). Use `DistinctBy(s => s.Name)` and order by name.

### Step 4: Add GetStatuses API endpoint

**What:** Create a new FastEndpoints endpoint that exposes Jira statuses to the frontend.

**Skill:** `create-feature` -- but this is simple enough to build directly following the GetBoards pattern.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Sync/GetStatuses/GetStatusesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetStatuses/GetStatusesQuery.cs`

**Pattern:** Follow `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs` exactly.

**Details:**
- Route: `GET /api/jira/statuses`
- `[AllowAnonymous]`, `[Tags("Sync")]`
- `EndpointWithoutRequest<GetStatusesResponse>`
- Response shape: `GetStatusesResponse { List<StatusDto> Statuses }` where `StatusDto { string Name, string CategoryKey }`
- Inject `IJiraClient`, call `GetStatusesAsync`, map to DTOs

### Step 5: Add frontend API function and types

**What:** Add the API call for statuses and the board fetch, plus TypeScript types.

**Files to modify:**
- `client/src/api/settings.ts` -- add `getBoards()` and `getStatuses()` functions
- `client/src/types/index.ts` -- add `BoardOption` and `StatusOption` interfaces

**Pattern:** Follow existing functions in `client/src/api/settings.ts` and `client/src/api/analytics.ts`.

**Details:**
- `getBoards()`: calls `GET /api/boards`, returns `Promise<{ boards: BoardOption[] }>` where `BoardOption { id: number, name: string, type: string }`
- `getStatuses()`: calls `GET /api/jira/statuses`, returns `Promise<{ statuses: StatusOption[] }>` where `StatusOption { name: string, categoryKey: string }`

### Step 6: Update SettingsView.vue with dropdowns

**What:** Replace the Board ID numeric input with a `<select>` dropdown and replace the Done Statuses free-text input with a dropdown-based picker. Both gracefully fall back to manual input on Jira errors.

**Files to modify:**
- `client/src/views/SettingsView.vue`

**Pattern:** Existing reactive state pattern in the same file.

**Details:**

**Board selector:**
- On mount, call `getBoards()`. Store result in a `boards` ref. Track loading/error state.
- If boards load successfully: render a `<select>` with `<option>` for each board (`value=board.id`, label=`board.name (board.type)`). Bind to `form.boardId`.
- If boards fail to load: fall back to the existing `<input type="number">` with a small warning text "Could not load boards from Jira."
- Pre-select the current `form.boardId` value if it matches a loaded board.

**Done Statuses picker:**
- On mount, call `getStatuses()`. Store result in a `statuses` ref. Track loading/error state.
- If statuses load successfully: replace the free-text `<input>` + Add button with a `<select>` dropdown + Add button. The `<select>` shows all available statuses (excluding those already in `form.doneStatuses`). Selecting + clicking Add appends the status to the list.
- Visually hint "done" category statuses (e.g., show them first or with a subtle marker) since those are the most likely candidates.
- If statuses fail to load: fall back to the existing free-text input, same "Could not load statuses from Jira" warning.
- Keep the existing chip display with remove buttons for already-added statuses -- only the input mechanism changes.

**Fetch timing:** Both `getBoards()` and `getStatuses()` should fire in parallel with `fetchSettings()` on mount. Do not block the settings form on dropdown data -- show it immediately, swap to dropdown when data arrives.

## Testing Strategy

- **Backend:** Verify `GET /api/jira/statuses` returns a list of statuses with name and categoryKey fields. Verify it returns a proper error (502) when Jira is unreachable.
- **Frontend board dropdown:** Load settings page, verify dropdown appears with boards, select a board, save, reload and verify selection persists. Disconnect Jira, verify fallback to numeric input.
- **Frontend status dropdown:** Load settings page, verify dropdown shows Jira statuses, add a status via dropdown, verify it appears as a chip, remove it, save. Disconnect Jira, verify fallback to free-text input.

## Open Questions

None.
