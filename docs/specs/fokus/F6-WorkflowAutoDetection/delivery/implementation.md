# Workflow Auto-Detection — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — added `TransitionEdge` record, `TransitionEdgeData` record, and `GetTransitionEdgesForDetectionAsync` method; non-bug ticket IDs fetched first, then edges grouped by (FromStatus, ToStatus), then ticket count and sprint count via two parallel lightweight queries
- `src/Services/Fokus/Fokus.API/Features/Settings/WorkflowDetectionService.cs` — pure algorithm class: graph construction from edges, bidirectional cycle resolution by dominant direction, reachability-to-terminal scoring via backward propagation, Kahn's topological sort on the dominant DAG, done statuses forced to tail, sidelined = statuses with no path to terminal or cycle survivors
- `src/Services/Fokus/Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesEndpoint.cs` — GET `/api/settings/workflow-stages/detect`, `EndpointWithoutRequest`, `[AllowAnonymous]`, `[Tags("Settings")]`, injects TicketRepository + AppSettingsRepository + WorkflowDetectionService
- `src/Services/Fokus/Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesQuery.cs` — `DetectWorkflowStagesResponse` and `DetectionConfidence` response records

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `services.AddScoped<WorkflowDetectionService>()` and the `Fokus.API.Features.Settings` using directive
- `client/src/types/index.ts` — added `DetectionConfidence` and `DetectionResult` interfaces
- `client/src/api/settings.ts` — added `detectWorkflowStages()` function calling `GET /settings/workflow-stages/detect`
- `client/src/stores/settingsStore.ts` — added `detectionResult`, `detecting` state; `runDetectWorkflowStages()`, `clearDetection()`, `moveSidelinedToStages()`, `removeSidelinedStatus()` actions
- `client/src/views/SettingsView.vue` — enhanced Workflow Stages section: Re-detect button with disabled logic, confidence summary line, empty-state message, sidelined "Other statuses" pill area with "+" move buttons; auto-detect on mount when stages empty; `save()` calls `store.clearDetection()` on success

## Key Decisions

- `GetTransitionEdgesForDetectionAsync` uses three queries (non-bug IDs, edges group-by, ticket/sprint counts) rather than one large join — SQLite handles joins poorly at scale and the dataset is small; the approach avoids EF expression-tree complexity with Contains on large sets
- `WorkflowDetectionService` is a plain class (no constructor dependencies) registered as `AddScoped` following the `SprintIssueSyncService` precedent
- `moveSidelinedToStages` in the store only removes from the sidelined list; the view (`moveSidelined`) handles appending to `form.workflowStages` — keeps form state in the view, store state in the store
- `reDetectDisabled` computed: disabled when `detecting` is true OR when `detectionResult` is not null and all confidence counts are zero (no transition data). When `detectionResult` is null (fresh page with stages), Re-detect is enabled per plan decision (A)
- Empty-state message ("Sync sprints to enable workflow detection") shows only when `form.workflowStages.length === 0` AND `detectionResult === null` AND NOT `detecting` — covers the no-data case without flickering during the auto-detect call

## Deviations from Plan

- None. All 6 steps implemented as specified.
