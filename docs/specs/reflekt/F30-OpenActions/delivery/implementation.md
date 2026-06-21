# Open Actions from Previous Retro — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.Domain/Cards/ReviewStatus.cs` — ReviewStatus enum (None, Done, CarriedOver, Dropped)
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/ReviewStatusChangedEvent.cs` — domain event carrying Card reference, implements IDomainEvent
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260519160025_AddReviewStatus.cs` — EF migration adding ReviewStatus (NOT NULL, default 0) and ReviewedInSprintId (nullable FK) to Cards, index on Sprints.ClosedAt
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/PreviousActionsContracts.cs` — shared PreviousActionItemResponse record used by GET and PATCH
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/GetPreviousActions/GetPreviousActionsEndpoint.cs` — GET /api/sprints/{sprintId}/previous-actions, OwnerOrScrumMaster auth
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs` — PATCH /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}, ScrumMaster auth, with validator
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/BroadcastReviewStatusChangedHandler.cs` — SignalR broadcast on ReviewStatusChangedEvent, sends reviewedCount/totalCount to reviewing sprint group
- `web/reflekt-web/src/stores/previousActions.ts` — Pinia store with state (items, loading, previousSprintName, previousSprintId), getters (totalCount, reviewedCount, unreviewedCount, allReviewed, summaryCounts), actions (fetchPreviousActions, applyReviewStatusChanged)
- `web/reflekt-web/src/components/retro/PreviousActionsPanel.vue` — collapsible review panel with SM status buttons, participant read-only badges, auto-collapse on allReviewed, tooltips from help.tooltips.md

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — added ReviewStatus and ReviewedInSprintId properties
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — added SetReviewStatus(status, reviewingSprintId) method with guards (None throws, non-ActionItem throws), raises ReviewStatusChangedEvent
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — added ReviewStatus (IsRequired, HasDefaultValue(None)), ReviewedInSprintId FK to Sprints (OnDelete SetNull), index on ReviewedInSprintId
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — added index on ClosedAt
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs` — added GetMostRecentClosedByTeamIdAsync
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs` — added GetUnreviewedActionItemsBySprintIdAsync, GetActionItemsReviewedInSprintAsync, and CountActionItemsBySprintIdAsync (cycle 1: avoids loading EvidenceLinks for count-only queries)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs` — added CardRepository injection, Writing phase branch returning UnreviewedActionCount/TotalPreviousActionCount; extended AdvancePhaseCheckResponse with two nullable int fields
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — added ReportPreviousActionsSection, ReportPreviousActionEntry records; added PreviousActionsReview optional field to ReportResponse
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — added BuildPreviousActionsSectionAsync helper and populated PreviousActionsReview in response
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — same BuildPreviousActionsSectionAsync helper added, populated PreviousActionsReview in response
- `web/reflekt-web/src/stores/sprint.ts` — added unreviewedActionCount and totalPreviousActionCount to AdvancePhaseCheckResponse interface
- `web/reflekt-web/src/stores/report.ts` — added ReportPreviousActionEntry and ReportPreviousActionsSection interfaces; added optional previousActionsReview field to ReportResponse; extended copyAsMarkdown to include the section
- `web/reflekt-web/src/composables/useSprintHub.ts` — added usePreviousActionsStore import and ReviewStatusChanged SignalR handler registration
- `web/reflekt-web/src/composables/useRetroRoom.ts` — added usePreviousActionsStore import and fetchPreviousActions call in enterRoom parallel load
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — added PreviousActionsPanel import and conditional render above column grid (Writing phase + items.length > 0)
- `web/reflekt-web/src/views/RetroRoomView.vue` — added Writing phase branch in onAdvanceClick showing unreviewed count confirmation dialog
- `web/reflekt-web/src/components/reports/SprintReportPanel.vue` — added Previous Actions Review section render and reviewStatusBadgeClass/formatReviewStatus helpers

## Key Decisions

- **SetReviewStatus same-status guard:** Added early return when both status and reviewingSprintId are unchanged, preventing spurious SignalR broadcasts on duplicate submissions. The 200 response from the endpoint is unaffected.
- **Report section includes both reviewed and unreviewed eligible items:** BuildPreviousActionsSectionAsync queries both GetActionItemsReviewedInSprintAsync (reviewed by this sprint) and GetUnreviewedActionItemsBySprintIdAsync (still unreviewed from previous sprint), combining both into the section. Unreviewed items show "Not reviewed".
- **BroadcastReviewStatusChangedHandler uses targeted queries:** Uses CountActionItemsBySprintIdAsync (count-only, no EvidenceLinks) + GetUnreviewedActionItemsBySprintIdAsync to derive reviewedCount without loading the full card graph.
- **AdvancePhaseCheck Writing branch uses targeted queries:** Replaced GetBySprintIdAsync (which includes EvidenceLinks) with GetUnreviewedActionItemsBySprintIdAsync + GetActionItemsReviewedInSprintAsync to avoid unnecessary eager loading.
- **PreviousActionsPanel uses optimistic update with revert:** Status button clicks update the item optimistically then call PATCH; on failure the previous status is restored.
- **reviewedCount/totalCount payload documented as intentionally unused:** Server-sent counts in ReviewStatusChanged are present for future use; local computed getters derive these values from items state, which is always consistent.

## Cycle 1 Fixes (reviewer findings)

- `Card.cs Behaviors` — added same-status early return to SetReviewStatus (MEDIUM: spurious domain event on no-op)
- `AdvancePhaseCheckEndpoint.cs` — replaced GetBySprintIdAsync with targeted queries; removed unused CardType import (MEDIUM: unnecessary EvidenceLinks load)
- `BroadcastReviewStatusChangedHandler.cs` — replaced GetBySprintIdAsync with CountActionItemsBySprintIdAsync + GetUnreviewedActionItemsBySprintIdAsync (MEDIUM: unnecessary EvidenceLinks load)
- `CardRepository.cs` — added CountActionItemsBySprintIdAsync count-only query method
- `RetroRoomView.vue` — wired advance warning tooltip from help.tooltips.md to Writing-phase dialog message paragraph (MEDIUM: tooltip absent)
- `previousActions.ts` — added explanatory comment on unused reviewedCount/totalCount payload fields (LOW)

## Deviations from Plan

- None. All steps implemented as specified.
