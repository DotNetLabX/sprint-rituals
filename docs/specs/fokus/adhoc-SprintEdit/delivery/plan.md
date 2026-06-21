# Sprint Edit

**Feature Spec:** None (user-directed, requirements from conversation + Jira mockup)

## Context

Sprint data is synced from Jira but there's no way to correct mistakes (wrong dates, typos in names, missing goals) from within Fokus. Users must switch to Jira to make edits. This feature adds an edit button to the sprint view that opens a dialog, and writes changes to both Jira (via the Agile REST API) and the local database.

## Scope

**In scope:**
- Add `Goal` property to Sprint domain entity (doesn't exist today)
- Add `Goal` to `JiraSprint` contract DTO (Jira returns it, we don't map it yet)
- Jira sprint update API call (`POST /rest/agile/1.0/sprint/{sprintId}` — partial update)
- New `PUT /api/sprints/{id}` endpoint — writes to Jira first, then updates local DB
- Edit button (pencil icon) on the sprint info line in `PageToolbar.vue`, visible only when a single sprint is selected
- Modal dialog with: Sprint name (text input, required), Start date (date input, required), End date (date input, required), Sprint goal (textarea, optional)
- Update `GetClosedSprints` to return `Goal`
- Refresh sprint list in store after successful edit

**Out of scope:**
- Editing sprint state (active/closed/future) — that's a Jira workflow action, not a field edit
- Editing memberships or tickets
- Protecting manually-edited fields from sync overwrite (accept overwrite for v1)
- Toast/notification system — use existing inline feedback pattern

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | Sprint aggregate, `Goal` property (string?, max 1024) | |
| 2 | persistence-patterns | Follow | SprintConfiguration, SprintRepository.UpsertAsync, Goal column | |
| 3 | (none) | — | Jira module: IJiraApi Refit method, IJiraClient, RestApiJiraClient | Log: no skill for Jira module write operations |
| 4 | persistence-patterns | Follow | EF Core migration for Goal column | |
| 5 | create-feature | Follow | UpdateSprint endpoint + validator, FastEndpoints vertical slice | |
| 6 | (none) | — | Update GetClosedSprints response DTO to include Goal | |
| 7 | vue-patterns | Follow | TypeScript types, API client function | |
| 8 | vue-component-architecture | Follow | SprintEditDialog modal component | |
| 9 | vue-patterns | Follow | PageToolbar edit button, dialog wiring, store refresh | |

## Domain Model Changes

**Modified aggregate: `Sprint`**
- Add `Goal` property: `public string? Goal { get; set; }` — nullable, max 1024 chars
- Update `Sprint.FromJira` to map `dto.Goal`

**Modified contract DTO: `JiraSprint`**
- Add `public string? Goal { get; set; }` — Jira already returns this field, just not mapped

## Data Model Changes

- Add `Goal` column (nvarchar, nullable, max 1024) to `Sprints` table
- EF Core migration

## Implementation Steps

### Step 1: Add Goal to domain model and Jira contract

**What:** Add `Goal` property to Sprint entity and JiraSprint DTO. Update the `FromJira` factory to map Goal.

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Sprint/Sprint.cs` — add `public string? Goal { get; set; }`
- `src/Modules/Jira/Jira.Contracts/JiraSprint.cs` — add `public string? Goal { get; set; }`
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — map `Goal = dto.Goal` in `FromJira`

**Skill:** Follow domain-patterns. Property is nullable, no invariants.

### Step 2: Update persistence layer for Goal

**What:** Configure Goal column in EF Core and update the repository's UpsertAsync to include Goal.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintConfiguration.cs` — add `builder.Property(s => s.Goal).HasMaxLength(1024)`
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — add `existing.Goal = sprint.Goal` in `UpsertAsync`

**Skill:** Follow persistence-patterns.
**Depends on:** Step 1.

### Step 3: Add Jira sprint update capability

**What:** Add a write method to the Jira module for updating a sprint via `POST /rest/agile/1.0/sprint/{sprintId}` (partial update). This is the first write operation to Jira in the codebase.

**Files to modify:**
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — add `Task<JiraSprint> UpdateSprintAsync(int sprintId, string name, DateTime startDate, DateTime endDate, string? goal, CancellationToken ct)`
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — add Refit method: `[Post("/rest/agile/1.0/sprint/{sprintId}")] Task<IApiResponse<JiraSprint>> UpdateSprintAsync(int sprintId, [Body] UpdateJiraSprintRequest request, CancellationToken ct)`

**Files to create:**
- `src/Modules/Jira/Jira.Contracts/UpdateJiraSprintRequest.cs` — request body DTO with `Name`, `StartDate`, `EndDate`, `Goal` (all nullable so Jira treats it as partial update). Use `DateTime?` for date fields — the Refit client is configured with `SystemTextJsonContentSerializer` and custom `JiraDateTimeConverter` which handles ISO 8601 serialization. System.Text.Json's default `DateTime` format produces ISO 8601 (`yyyy-MM-ddTHH:mm:ss`), which Jira accepts. Verify this works on the first manual test since this is the first write to Jira.

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — implement `UpdateSprintAsync`: build `UpdateJiraSprintRequest`, call `RequestAsync(() => Api.UpdateSprintAsync(...))`, return result

**Skill:** None — no skill covers Jira module write operations.
**Pattern reference:** Follow `RestApiJiraClient.GetSprintsAsync` for the `RequestAsync` wrapper pattern.
**Depends on:** Step 1.

### Step 4: EF Core migration

**What:** Add migration for the new Goal column on Sprint.

**Command:**
```bash
dotnet ef migrations add AddSprintGoal -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

**Skill:** Follow persistence-patterns.
**Depends on:** Step 2.

### Step 5: Create UpdateSprint API endpoint

**What:** New `PUT /api/sprints/{sprintId}` endpoint. Admin-only. Flow: validate request → call `IJiraClient.UpdateSprintAsync` (Jira first) → update local Sprint entity → save → return updated sprint.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintEndpoint.cs` — FastEndpoints endpoint. Override `Configure()` with `Roles("Admin")` to match the codebase's write-endpoint authorization pattern (see `SaveSettingsEndpoint`, `SyncSprintsEndpoint`).
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintModels.cs` — `UpdateSprintRequest` (int SprintId route param, string Name, DateTime StartDate, DateTime EndDate, string? Goal) + `UpdateSprintResponse` (Id, Name, StartDate, EndDate, Goal, State). Route parameter is `{sprintId}` to match the `SprintId` request property.
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintValidator.cs` — Name not empty, StartDate < EndDate

**Endpoint handler logic:**
1. Load sprint from `SprintRepository.GetByIdAsync(req.SprintId)` — 404 if not found
2. Call `IJiraClient.UpdateSprintAsync(sprint.Id, req.Name, req.StartDate, req.EndDate, req.Goal)` — if Jira fails, the exception propagates (BadGateway/Unauthorized handled by GlobalExceptionMiddleware)
3. Update local entity: Name, StartDate, EndDate, Goal
4. Call `sprintRepository.SaveChangesAsync`
5. Return `UpdateSprintResponse`

**Skill:** Follow create-feature for the vertical slice layout.
**Pattern reference:** `GetClosedSprintsEndpoint.cs` for endpoint structure, `SaveSettingsEndpoint` for PUT + auth pattern.
**Depends on:** Steps 2, 3.

### Step 6: Update GetClosedSprints to include Goal

**What:** Add Goal to the response DTO so the frontend has it for the edit dialog's initial values.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsQuery.cs` — add `public string? Goal { get; set; }` to `ClosedSprintItem`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsEndpoint.cs` — map `Goal = s.Goal` in the Select projection

**Depends on:** Step 1.

### Step 7: Frontend types and API client

**What:** Add Goal to the TypeScript type and create the updateSprint API function.

**Files to modify:**
- `client/src/types/index.ts` — add `goal?: string` to `ClosedSprintItem`
- `client/src/api/analytics.ts` — add `updateSprint(sprintId: number, req: UpdateSprintRequest): Promise<UpdateSprintResponse>` using `apiFetch` with method PUT. Add `UpdateSprintRequest` and `UpdateSprintResponse` interfaces.

**Skill:** Follow vue-patterns for API client conventions.
**Depends on:** Steps 5, 6.

### Step 8: Create SprintEditDialog component

**What:** Modal dialog component with form fields matching the Jira mockup. Uses `<teleport to="body">` for overlay. Design-token classes for styling. Dialog title: "Edit sprint: {sprint.name}".

**Files to create:**
- `client/src/components/sprints/SprintEditDialog.vue`

**Props:** `sprint: ClosedSprintItem` (pre-fills form), `open: boolean`
**Emits:** `close`, `saved` (with updated sprint data)

**Dialog behavior:**
- Click backdrop (outside dialog) → close
- Press Escape → close
- z-index above sidebar (use `z-50`)

**Form fields:**
- Sprint name — `<input type="text">`, required (marked with asterisk), pre-filled from `sprint.name`
- Start date — `<input type="date">`, required (marked with asterisk), pre-filled from `sprint.startDate`
- End date — `<input type="date">`, required (marked with asterisk), pre-filled from `sprint.endDate`
- Sprint goal — `<textarea>`, optional, pre-filled from `sprint.goal`

**State:** `reactive` form object, `saving` ref, `error` ref. Follow `SettingsView.vue` pattern for submit handler (try/catch/finally with saving flag). Reset form values from props when dialog opens (watch `open`).

**Buttons:** Cancel (closes dialog), Update (submits, disabled while saving, shows "Updating..." text).

**Error display:** Inline `<span>` with error message below buttons, same pattern as SettingsView.

**Skill:** Follow vue-component-architecture for component structure.
**Depends on:** Step 7.

### Step 9: Add edit button to PageToolbar and wire dialog

**What:** Add a pencil icon button next to the sprint info line. Clicking opens `SprintEditDialog`. On successful save, refresh the sprint list in the store. The edit button is opt-in via a new `showEditButton` prop (default `false`) to avoid coupling the dialog into all 5 views that use PageToolbar.

**Files to modify:**
- `client/src/components/PageToolbar.vue` — add `showEditButton` prop (default `false`). Add pencil icon button in the sprint info line (line 157-165), visible only when `showEditButton && selectedSprint`. Add `editDialogOpen` ref. Import and render `SprintEditDialog` (conditionally, only when `showEditButton`). Add new emit `sprintUpdated`.
- `client/src/stores/sprintsStore.ts` — add `refreshSprints()` action that re-fetches `getClosedSprints()` and updates `closedSprints` ref.
- `client/src/views/SprintsView.vue` — pass `showEditButton` to PageToolbar. Handle `@sprintUpdated` by calling `await store.refreshSprints()` then `await store.fetchAllData()` (sequential, not parallel — sprints must refresh before analytics re-fetch).

**Skill:** Follow vue-patterns for event handling, pinia-patterns for store action.
**Depends on:** Step 8.

## Migration Notes

```bash
dotnet ef migrations add AddSprintGoal -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
dotnet ef database update -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

No seed data needed — Goal is nullable, existing rows get NULL.

## Testing Strategy

1. **Jira write-through:** Edit a sprint in Fokus → verify the change appears in Jira
2. **Local DB update:** After edit, verify the sprint list shows updated values without re-sync
3. **Validation:** Submit with empty name → error. Submit with startDate > endDate → error.
4. **Jira failure handling:** If Jira returns 401/400, the dialog shows the error, local DB is NOT updated
5. **Dialog UX:** Open dialog → fields pre-filled. Cancel → no changes. Edit → Update button shows "Updating..." → dialog closes on success.
6. **Goal field round-trip:** Set a goal → save → re-open dialog → goal is there. Clear goal → save → goal is empty.
7. **Sync overwrite acceptance:** Edit a sprint, then re-sync from Jira → Jira values overwrite local edits (expected v1 behavior).

## Open Questions

None — requirements are clear from the user's mockup and conversation.
