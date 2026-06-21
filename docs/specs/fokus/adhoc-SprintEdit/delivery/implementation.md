# Sprint Edit — Implementation

## Files Created

- `src/Modules/Jira/Jira.Contracts/UpdateJiraSprintRequest.cs` — Request body DTO for Jira sprint update (Name, StartDate, EndDate, Goal — all nullable for partial update semantics)
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintModels.cs` — `UpdateSprintRequest` (route param SprintId + body fields) and `UpdateSprintResponse`
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintValidator.cs` — Validates Name not empty, StartDate < EndDate
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintEndpoint.cs` — PUT /api/sprints/{sprintId}, Admin-only, Jira-first write-through
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260510104427_AddSprintGoal.cs` — EF Core migration adding Goal column (nullable, nvarchar(1024))
- `client/src/components/sprints/SprintEditDialog.vue` — Modal dialog with form fields, backdrop/Escape close, saving state, inline error

## Files Modified

- `src/Services/Fokus/Fokus.Domain/Sprint/Sprint.cs` — Added `public string? Goal { get; set; }`
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — Added `Goal = dto.Goal` in `FromJira` factory
- `src/Modules/Jira/Jira.Contracts/JiraSprint.cs` — Added `public string? Goal { get; set; }`
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — Added `UpdateSprintAsync` to the interface
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — Added Refit `[Post]` method for `/rest/agile/1.0/sprint/{sprintId}`
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — Implemented `UpdateSprintAsync` using `RequestAsync` wrapper
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintConfiguration.cs` — Added `Goal` column config with `HasMaxLength(1024)`
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — Added `existing.Goal = sprint.Goal` in `UpsertAsync`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsQuery.cs` — Added `public string? Goal { get; set; }` to `ClosedSprintItem`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsEndpoint.cs` — Added `Goal = s.Goal` to the Select projection
- `client/src/types/index.ts` — Added `goal?: string` to `ClosedSprintItem` interface
- `client/src/api/analytics.ts` — Added `UpdateSprintRequest`, `UpdateSprintResponse` interfaces, and `updateSprint` function
- `client/src/stores/sprintsStore.ts` — Added `refreshSprints()` action that re-fetches closed sprints list
- `client/src/components/PageToolbar.vue` — Added `showEditButton` prop (default false), pencil icon button, `editDialogOpen` ref, `sprintUpdated` emit, `SprintEditDialog` wiring
- `client/src/views/SprintsView.vue` — Added `show-edit-button` prop, `@sprint-updated` handler calling `refreshSprints` then `fetchAllData` sequentially

## Key Decisions

- Auth pattern: Used `[Authorize(Roles = "Admin")]` attribute (not `Roles()` in `Configure()`) — this matches every other write endpoint in the codebase. A linter corrected the initial `Configure()` approach.
- Date serialization: `UpdateJiraSprintRequest` uses `DateTime?` fields. Refit with `SystemTextJsonContentSerializer` serializes as ISO 8601, which Jira accepts. Needs first manual test to confirm.
- Date input pre-fill: `toDateInput()` helper slices the ISO string to `yyyy-MM-dd` (first 10 chars) for `<input type="date">` compatibility.
- `showEditButton` defaults to `false` — opt-in per view, so the dialog is not loaded in Dashboard, Developers, Epics, or Cycle Time views.

## Deviations from Plan

- None. All 9 steps implemented as specified.
