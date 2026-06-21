# Open Actions from Previous Retro

**Feature Spec:** `docs/features/OpenActions/spec.md`

## Context

Retros currently start from zero with no connection to previous sprints. Action items are created, discussed, and forgotten. This feature adds an action item review section at the top of the sprint board during the Writing phase, giving the team continuity across retros and holding the SM accountable for follow-through. The SM reviews each previous action item as Done, Carried Over, or Dropped. Review statuses are persisted on the original cards (cross-sprint traceability without duplication) and broadcast in real-time.

**Services impacted:** Reflekt (sole service). Changes span Domain (Card aggregate gains two fields + behavior), Persistence (migration + repository queries), API (two new endpoints, two modified endpoints, one new domain event handler), and Frontend (new store, new component, hub wiring, report extension).

## Scope

**In scope:**
- Card aggregate gains `ReviewStatus` enum and `ReviewedInSprintId` nullable int
- GET endpoint to fetch previous action items for a sprint
- PATCH endpoint to set review status (SM-only, Writing phase only)
- AdvancePhaseCheck endpoint extended for Writing phase unreviewed-item counts
- Report endpoints extended with "Previous Actions Review" section
- ReviewStatusChanged SignalR event
- Frontend: collapsible review section above the board during Writing phase
- Frontend: auto-collapse with summary when all reviewed
- Frontend: SM sees status buttons, participants see read-only
- Frontend: advance phase confirmation dialog for unreviewed items
- Help tooltips wired to UI elements per `docs/features/OpenActions/help.tooltips.md`

**Out of scope (per spec):**
- Auto-copy on Carried Over
- Persistent action item tracker / dashboard
- Review during phases other than Writing
- Participant-initiated review statuses
- Action items from multiple previous sprints
- Hard gate on unreviewed items
- Clearing / resetting a review status to None

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | Card aggregate: ReviewStatus enum, ReviewedInSprintId property, SetReviewStatus behavior | |
| 2 | persistence-patterns | Follow | CardConfiguration: 2 new columns, SprintConfiguration: index on ClosedAt, migration | |
| 3 | persistence-patterns | Follow | CardRepository + SprintRepository: new query methods | |
| 4 | create-feature | Follow | GetPreviousActions endpoint (GET, participant auth) | |
| 5 | create-feature | Follow | SetReviewStatus endpoint (PATCH, SM auth) | |
| 6 | create-domain-event-handler | Follow | BroadcastReviewStatusChangedHandler (SignalR) | |
| 7 | (none) | — | AdvancePhaseCheck endpoint extension for Writing phase | |
| 8 | (none) | — | Report endpoints extension with previousActionsReview section | |
| 9 | pinia-patterns, vue-patterns | Follow | previousActions store + useSprintHub wiring | |
| 10 | vue-component-architecture, vue-patterns | Follow | PreviousActionsPanel component + RetroRoomView/RetroBoardPanel integration | |
| 11 | vue-patterns | Follow | Report store + SprintReportPanel extension for previous actions review section | |

## Domain Model Changes

**Card (modified):**
- `ReviewStatus` — new enum `ReviewStatus { None, Done, CarriedOver, Dropped }` in `Reflekt.Domain.Cards`.
- `ReviewedInSprintId` — `int?`, nullable FK to Sprint. Null until reviewed.
- New behavior method `SetReviewStatus(ReviewStatus status, int reviewingSprintId)` on the Card partial class. Guards: status cannot be `None` (no un-reviewing), CardType must be ActionItem. Sets both fields and raises `ReviewStatusChangedEvent`.

**New domain event:**
- `ReviewStatusChangedEvent(Card Card)` — follows existing pattern (`CardCreatedEvent`, etc.). Implements `IDomainEvent`.

**No new aggregates.** ReviewStatus is persisted on the existing Card aggregate in the previous sprint. No card duplication.

## Data Model Changes

**Cards table** gains two columns:

| Column | Type | Nullable | Default |
|--------|------|----------|---------|
| ReviewStatus | INTEGER (enum) | NOT NULL | 0 (None) |
| ReviewedInSprintId | INTEGER | NULL | NULL |

**Sprints table** gains an index:
- Index on `ClosedAt` (for efficient "most recently closed sprint in team" query).

FK: `Cards.ReviewedInSprintId` references `Sprints.Id`, `OnDelete(SetNull)` (if the reviewing sprint is deleted, the card reverts to unreviewed — defensive, unlikely scenario).

Non-breaking migration — existing rows get `ReviewStatus = 0` (None) and `ReviewedInSprintId = NULL`.

## Implementation Steps

### Step 1: Domain — ReviewStatus enum + Card fields + behavior

Follow `domain-patterns`.

**Create:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/ReviewStatus.cs` — enum `ReviewStatus { None, Done, CarriedOver, Dropped }`.
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/ReviewStatusChangedEvent.cs` — `sealed record ReviewStatusChangedEvent(Card Card) : IDomainEvent`.

**Modify:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — add two properties:
  - `ReviewStatus ReviewStatus { get; private set; } = ReviewStatus.None;`
  - `int? ReviewedInSprintId { get; private set; }`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — add method `SetReviewStatus(ReviewStatus status, int reviewingSprintId)`:
  - Guard: `status == ReviewStatus.None` throws (cannot un-review).
  - Guard: `CardType != CardType.ActionItem` throws (only action items are reviewable).
  - Sets `ReviewStatus = status` and `ReviewedInSprintId = reviewingSprintId`.
  - Raises `ReviewStatusChangedEvent(this)`.

**Accept:** Card has ReviewStatus and ReviewedInSprintId. SetReviewStatus validates, mutates, and raises event. Setting None throws. Non-ActionItem throws.

**Depends on:** nothing.

### Step 2: Persistence — EF configuration + migration

Follow `persistence-patterns`.

**Modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`:
  - Configure `ReviewStatus`: `.IsRequired()`, store as integer (enum default).
  - Configure `ReviewedInSprintId`: `.HasOne<Sprint>().WithMany().HasForeignKey(c => c.ReviewedInSprintId).OnDelete(DeleteBehavior.SetNull)`.
  - Add index on `ReviewedInSprintId`.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`:
  - Add index on `ClosedAt` for efficient "most recent closed sprint" queries.

**Create:**
- EF migration: `dotnet ef migrations add AddReviewStatus -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API`

**Accept:** Migration creates two new columns on Cards (ReviewStatus NOT NULL default 0, ReviewedInSprintId nullable FK). Index on Sprints.ClosedAt. Existing data unaffected. `dotnet ef database update` succeeds.

**Depends on:** Step 1.

### Step 3: Persistence — repository query methods

Follow `persistence-patterns`.

**Modify:**
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs` — add method:
  - `GetMostRecentClosedByTeamIdAsync(int teamId, CancellationToken ct)` — returns the sprint with the latest `ClosedAt` where `TeamId == teamId` and `State == Closed`. Returns `Sprint?`.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs` — add methods:
  - `GetUnreviewedActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)` — returns cards where `SprintId == sprintId`, `CardType == ActionItem`, and `ReviewedInSprintId == null`. Include participant info (AssigneeName is on the card, so no join needed).
  - `GetActionItemsReviewedInSprintAsync(int reviewingSprintId, CancellationToken ct)` — returns cards where `ReviewedInSprintId == reviewingSprintId`. Used by the report endpoint.

**Accept:** SprintRepository can find the most recently closed sprint for a team. CardRepository can find unreviewed action items for a sprint and action items reviewed in a specific sprint.

**Depends on:** Steps 1, 2.

### Step 4: API — GET previous actions endpoint

Follow `create-feature`.

**Create feature folder:** `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/GetPreviousActions/`

**Create:**
- `GetPreviousActionsEndpoint.cs` — endpoint + request + response records in one file (follows existing pattern like `GetSprintReport`).
  - Route: `GET /api/sprints/{sprintId}/previous-actions`
  - Auth: `OwnerOrScrumMaster` policy (any participant or SM can read).
  - Request: `record GetPreviousActionsRequest(int SprintId)`.
  - Response: `List<PreviousActionItemResponse>` where `PreviousActionItemResponse` is `record(int CardId, string Text, string? AssigneeName, string ReviewStatus)`.
  - Logic: Load sprint, validate exists. Load sprint's team. Call `SprintRepository.GetMostRecentClosedByTeamIdAsync(teamId)`. If no closed sprint, return empty array. Call `CardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprintId)`. Filter to items not yet reviewed by any sprint. Map and return.
  - Returns 200 (array, possibly empty), 404 (sprint not found).

**Also create:**
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/PreviousActionsContracts.cs` — shared response records used by both GET and PATCH endpoints:
  - `PreviousActionItemResponse(int CardId, string Text, string? AssigneeName, string ReviewStatus, int PreviousSprintId, string PreviousSprintName)`

**Accept:** GET returns action items from the most recently closed sprint in the same team. Empty array when no previous sprint, no action items, or all reviewed. 404 when sprint not found. Participant-accessible.

**Depends on:** Steps 1-3.

### Step 5: API — PATCH set review status endpoint

Follow `create-feature`.

**Create feature folder:** `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/`

**Create:**
- `SetReviewStatusEndpoint.cs` — endpoint + command + validator.
  - Route: `PATCH /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}`
  - Auth: `ScrumMaster` policy.
  - Command: `record SetReviewStatusCommand(int TeamId, int SprintId, int CardId, string ReviewStatus)`.
  - Validator: `ReviewStatus` must be one of `Done`, `CarriedOver`, `Dropped`.
  - Logic:
    - Load team, verify SM ownership.
    - Load current sprint, verify belongs to team, verify Active state, verify Writing phase (409 if not).
    - Determine the expected previous sprint: `SprintRepository.GetMostRecentClosedByTeamIdAsync(teamId)`.
    - Load the card by CardId. Verify card belongs to the previous sprint (404 if not).
    - Parse ReviewStatus string to enum.
    - Call `card.SetReviewStatus(parsedStatus, sprintId)`.
    - SaveChangesAsync (triggers domain event dispatch).
    - Return updated `PreviousActionItemResponse`.
  - Status codes: 200, 400, 403, 404, 409.

**Accept:** SM can set review status on previous action items. Non-SM gets 403. Non-Writing phase gets 409. Card not from previous sprint gets 404. Invalid status value gets 400. Status change persisted on original card. Same status is no-op (200). Domain event raised on change.

**Depends on:** Steps 1-3.

### Step 6: API — SignalR broadcast handler for ReviewStatusChanged

Follow `create-domain-event-handler`.

**Create:**
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/BroadcastReviewStatusChangedHandler.cs`
  - Handles `ReviewStatusChangedEvent`.
  - Injects `IHubContext<SprintHub>`, `CardRepository`.
  - Broadcasts `ReviewStatusChanged` to `sprint:{card.ReviewedInSprintId}` group (the reviewing sprint's group, where participants are connected).
  - Payload: `{ cardId, reviewStatus, reviewedCount, totalCount }`.
  - To compute `reviewedCount` and `totalCount`: query the previous sprint's action items. Count total action items and those with `ReviewedInSprintId != null`. This gives the frontend the data it needs for the auto-collapse summary.

**Accept:** Review status changes broadcast to all participants in the current sprint room. Payload includes counts for auto-collapse logic. Non-reviewing-sprint clients are unaffected.

**Depends on:** Steps 1, 5.

### Step 7: API — extend AdvancePhaseCheck for Writing phase

No matching skill. Modification of existing endpoint.

**Modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs`:
  - Add `CardRepository` to constructor injection.
  - Add a new branch for `SprintPhase.Writing`:
    - Find the most recently closed sprint in the same team via `SprintRepository.GetMostRecentClosedByTeamIdAsync`.
    - If a previous sprint exists, count unreviewed action items and total action items from that sprint.
    - Return `UnreviewedActionCount` and `TotalPreviousActionCount` in the response.
  - Extend `AdvancePhaseCheckResponse` record with two new nullable fields: `int? UnreviewedActionCount`, `int? TotalPreviousActionCount`.

**Accept:** AdvancePhaseCheck returns unreviewed action item counts when current phase is Writing and previous action items exist. Frontend uses these counts for the confirmation dialog. Null when no previous actions exist (no dialog needed). Existing Voting and Discussing checks unchanged.

**Depends on:** Steps 2, 3.

### Step 8: API — extend report endpoints with Previous Actions Review section

No matching skill. Modification of existing endpoints.

**Modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — add new records:
  - `ReportPreviousActionsSection(string PreviousSprintName, List<ReportPreviousActionEntry> Items)` — nullable section.
  - `ReportPreviousActionEntry(string Text, string? AssigneeName, string ReviewStatus)` — status is "Done", "CarriedOver", "Dropped", or "Not reviewed".
  - Add `ReportPreviousActionsSection? PreviousActionsReview` to `ReportResponse`.

- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs`:
  - After building the existing response, check if any action items were reviewed during this sprint (`CardRepository.GetActionItemsReviewedInSprintAsync(sprintId)`).
  - Also find the previous sprint to get its name and to find eligible items that were NOT reviewed.
  - Build the section: items with ReviewedInSprintId == this sprint show their status; items from the same previous sprint with ReviewedInSprintId == null show "Not reviewed".
  - Only include the section when at least one action item from the previous sprint was eligible for review.

- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs`:
  - Same logic as GetSprintReport — both share `ReportResponse` contract.

**Accept:** Reports include a "Previous Actions Review" section when action items were eligible for review. Each entry shows text, assignee, and status. Items not reviewed by the SM appear as "Not reviewed". Section identifies the source sprint by name. Non-breaking — clients that ignore the field work unchanged.

**Depends on:** Steps 2, 3.

### Step 9: Frontend — previousActions store + SignalR wiring

Follow `pinia-patterns` and `vue-patterns`.

**Create:**
- `web/reflekt-web/src/stores/previousActions.ts`:
  - State: `items` (array of previous action items), `loading`, `previousSprintName`, `previousSprintId`.
  - Computed: `totalCount`, `reviewedCount`, `unreviewedCount`, `allReviewed`, `summaryCounts` (done/carriedOver/dropped counts).
  - Actions: `fetchPreviousActions(sprintId)` — calls GET `/api/sprints/{sprintId}/previous-actions`. `applyReviewStatusChanged(payload)` — updates a single item's status and counts from the SignalR event.
  - Export the `PreviousActionItem` type interface.

**Modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts`:
  - Import `usePreviousActionsStore`.
  - Register `connection.on('ReviewStatusChanged', (payload) => previousActionsStore.applyReviewStatusChanged(payload))`.
- `web/reflekt-web/src/composables/useRetroRoom.ts`:
  - Import `usePreviousActionsStore`.
  - In `enterRoom()`, add `previousActionsStore.fetchPreviousActions(sprintId)` to the initial data load.
- `web/reflekt-web/src/stores/sprint.ts`:
  - Add `UnreviewedActionCount` and `TotalPreviousActionCount` to `AdvancePhaseCheckResponse` interface.

**Accept:** Store fetches previous actions on room entry. SignalR updates arrive in real-time. Advance phase check response includes action counts.

**Depends on:** Steps 4, 6.

### Step 10: Frontend — PreviousActionsPanel component + board integration

Follow `vue-component-architecture` and `vue-patterns`.

**Create:**
- `web/reflekt-web/src/components/retro/PreviousActionsPanel.vue` — L1 feature component:
  - Collapsible section with header: "Actions from last sprint (N)".
  - Each item shows: card text, assignee name, and three status buttons (Done / Carried Over / Dropped) for SM, or read-only status badges for participants.
  - Status buttons call PATCH `/api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}` with the selected status. Use guest token header for auth.
  - Active status button is visually highlighted (selected state).
  - When `allReviewed` is true, auto-collapse and show summary line: "All N actions reviewed (X done, Y carried over, Z dropped)".
  - SM can re-expand after auto-collapse.
  - Wire tooltips from `docs/features/OpenActions/help.tooltips.md`:
    - Section header tooltip: "Review action items from your team's previous retro..."
    - Done button tooltip: "Mark this action item as completed since the last retro."
    - Carried Over button tooltip: "Mark this action item as still in progress..."
    - Dropped button tooltip: "Mark this action item as no longer relevant or needed."
    - Summary line tooltip: "All previous action items have been reviewed..."
  - Props: `sprintId`, `teamId`, `isScrumMaster`.
  - Reads from `usePreviousActionsStore()`.

**Modify:**
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue`:
  - Import and render `PreviousActionsPanel` above the column grid, only during Writing phase and when `previousActionsStore.items.length > 0`.
  - Pass `sprintId`, `teamId`, `isScrumMaster` props.

- `web/reflekt-web/src/views/RetroRoomView.vue`:
  - Extend the `onAdvanceClick` function: add a branch for `check.currentPhase === 'Writing'` that uses `unreviewedActionCount` and `totalPreviousActionCount` to show the confirmation dialog: "N of M previous action items haven't been reviewed yet. Advance anyway?"
  - Wire the advance warning tooltip from help.tooltips.md.

**Accept:** Review section visible during Writing when previous actions exist. Hidden in all other phases. SM sees status buttons, participants see read-only. Auto-collapses with summary when all reviewed. Re-expandable. Advance confirmation dialog shows unreviewed count. Tooltips match help.tooltips.md. Does not block note-writing below.

**Depends on:** Steps 7, 9.

### Step 11: Frontend — report extension for Previous Actions Review

Follow `vue-patterns`.

**Modify:**
- `web/reflekt-web/src/stores/report.ts`:
  - Add `ReportPreviousActionsSection` and `ReportPreviousActionEntry` interfaces.
  - Add optional `previousActionsReview` field to `ReportResponse` interface.
  - Update `copyAsMarkdown` to include the previous actions review section if present.

- `web/reflekt-web/src/components/reports/SprintReportPanel.vue`:
  - Render a "Previous Actions Review" section when `report.previousActionsReview` exists.
  - Show section header: "Actions from: {previousSprintName}".
  - List each item with text, assignee, and status badge (Done / Carried Over / Dropped / Not reviewed).

**Accept:** Report view shows Previous Actions Review section when data exists. Each entry displays text, assignee, and status. "Not reviewed" shown for items the SM did not review. Markdown export includes the section. Section not rendered when absent (non-breaking).

**Depends on:** Step 8.

## Migration Notes

```bash
# Add migration (from repo root)
dotnet ef migrations add AddReviewStatus -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API

# Apply migration
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

Migration adds `ReviewStatus` (NOT NULL, default 0) and `ReviewedInSprintId` (nullable FK) to Cards table. Adds index on `Sprints.ClosedAt`. Existing data unaffected.

## Testing Strategy

**Domain:**
- `Card.SetReviewStatus(Done, sprintId)` — sets both fields, raises event.
- `Card.SetReviewStatus(None, sprintId)` — throws (cannot un-review).
- `Card.SetReviewStatus(Done, sprintId)` on a Note card — throws (only ActionItems).
- `Card.SetReviewStatus(CarriedOver, sprintId)` after already Done — replaces status (allowed).

**GET endpoint:**
- Sprint with previous closed sprint having unreviewed action items — returns them.
- Sprint with no previous closed sprint — returns empty array.
- Sprint with previous sprint where all action items reviewed — returns empty array.
- Action items already reviewed by another sprint — excluded.
- Sprint not found — 404.

**PATCH endpoint:**
- SM sets status during Writing — 200, status persisted.
- SM changes status (Done to CarriedOver) — 200, status replaced.
- SM sets same status again — 200 (no-op).
- Non-SM caller — 403.
- Non-Writing phase — 409.
- Card not from previous sprint — 404.
- Invalid status value — 400.
- Sprint not found — 404.

**AdvancePhaseCheck:**
- Writing phase with unreviewed items — returns counts.
- Writing phase with no previous actions — returns null counts.
- Existing Voting and Discussing checks — unchanged behavior.

**Report:**
- Sprint where actions were reviewed — section present with correct statuses.
- Sprint where no actions were eligible — section absent.
- Unreviewed eligible items appear as "Not reviewed".

**Frontend:**
- Review section appears during Writing with previous actions, hidden otherwise.
- SM sees buttons, participant sees read-only badges.
- Status click updates UI optimistically and calls PATCH.
- SignalR broadcasts update other participants in real-time.
- Auto-collapse triggers when all reviewed, re-expandable.
- Advance dialog shows unreviewed count during Writing.

## KB Impact

None — this is a cross-sprint feature that extends existing Card and Sprint concepts. The business rules are fully captured in the spec and do not require separate KB entries beyond what the spec already documents.

## Open Questions

None.
