# Workflow Auto-Detection

**Feature Spec:** `docs/features/WorkflowAutoDetection/spec.md`

## Context

F12 (Cycle Time) needs an ordered list of workflow stages to measure how long tickets spend in each phase. Manually configuring these stages requires the user to know their Jira workflow by heart. This feature analyzes the status transition history already collected during sprint sync and proposes an ordered workflow pipeline. The user reviews, edits, and confirms -- no guessing, no Jira admin access needed.

**Service impacted:** Fokus (single service). Backend: one new read-only endpoint + detection algorithm. Frontend: enhancement to the existing Settings view's Workflow Stages section.

## Scope

**In scope:**
- `GET /api/settings/workflow-stages/detect` endpoint returning detected stages, sidelined statuses, and confidence metadata
- Transition graph construction from StatusTransition data, excluding bugs
- Forward-flow scoring algorithm using configured done statuses as terminal anchors
- Cycle resolution by dominant direction
- Frontend: auto-detection trigger when workflow stages are empty on Settings visit
- Frontend: confidence summary display
- Frontend: "Other statuses" (sidelined) area with ability to move statuses into the main list
- Frontend: "Re-detect" button (disabled when no data)
- Frontend: proposal discard on navigate-away without save

**Out of scope (per spec):**
- Per-issue-type workflow pipelines
- Auto-grouping of similar status names
- Jira workflow definition API import
- Webhook-triggered or scheduled detection
- Status category import from Jira
- Confidence-based blocking (detection always returns a result)
- Cycle time boundary selection (F12's concern)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | New query method on TicketRepository for transition edge aggregation | |
| 2 | (none) | -- | Detection algorithm: graph construction, forward-flow scoring, ordering. Domain-specific, no reusable skill. | Logged in architecture v2 Gaps |
| 3 | create-feature | Follow | FastEndpoints GET endpoint, EndpointWithoutRequest, Settings domain area | |
| 4 | (none) | -- | Frontend API function + types | Frontend patterns gap |
| 5 | (none) | -- | Pinia store extension with detection state | Frontend patterns gap |
| 6 | (none) | -- | Vue template: auto-detect, confidence, sidelined area, re-detect | Frontend patterns gap |

## Domain Model Changes

None. No new entities, value objects, or domain events. The feature reads existing StatusTransition, Ticket, SprintMembership, and AppSettings data.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Add transition edge query to TicketRepository

**What:** Add a method to `TicketRepository` that returns the raw data the detection algorithm needs: aggregated transition edges (from-status, to-status, occurrence count) excluding transitions from tickets whose `IssueType` is `"Bug"`, plus confidence metadata (distinct non-bug ticket count with transitions, distinct sprint count covering those tickets, total transition count).

**Why a repository method:** Endpoints and services consume repositories, not `FokusDbContext` directly. The existing `TicketRepository` already accesses `DbContext.StatusTransitions` directly (see `ReplaceTransitionsAsync`), so adding a read query here is consistent.

**Query shape:** Join `StatusTransitions` with `Tickets` (to filter `IssueType != "Bug"`) and with `SprintMemberships` (to count distinct sprints). The transition edges should be grouped by `(FromStatus, ToStatus)` with a count. The confidence metadata (ticket count, sprint count, transition count) should be computed in the same query round-trip or as a parallel query to avoid N+1.

**Performance approach:** Single bulk query with GroupBy for edges. Confidence counts via a second lightweight query (or computed from the same result set in memory -- the dataset is small per the architecture doc).

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` -- add `GetTransitionEdgesForDetectionAsync` method

**Return type:** A DTO record (define in the repository file or a sibling file) containing:
- `List<TransitionEdge> Edges` where `TransitionEdge` is `record(string FromStatus, string ToStatus, int Count)`
- `int TransitionCount` (total transitions analyzed)
- `int TicketCount` (distinct non-bug tickets with transitions)
- `int SprintCount` (distinct sprints covering those tickets)

**Skill:** Follow `persistence-patterns` (repository query method pattern).

**Dependencies:** None.

---

### Step 2: Create WorkflowDetectionService

**What:** Create a focused operation service that takes the transition edge data and configured done statuses, and produces the detection result: an ordered list of workflow stages, a list of sidelined statuses, and the confidence metadata pass-through.

**Why a service:** The detection algorithm (graph construction, forward-flow scoring, cycle resolution, ordering) is too complex for inline endpoint handler logic. Per the Fokus service convention, extract into a focused operation service named after the operation, living in the feature area root. Single-purpose: detection only.

**Algorithm (full inline detail -- no skill covers this):**

1. **Build the transition graph.** Each unique status name is a node. Each `TransitionEdge` (from, to, count) is a weighted directed edge.

2. **Identify done statuses.** Read `AppSettings.DoneStatuses`. These are the known terminal nodes. Any status in the transition data that matches a done status is marked as terminal.

3. **Score each non-terminal status by forward-flow ratio.** For each status S:
   - Compute `forward_weight` = sum of edge weights FROM S toward statuses that are closer to terminals (transitively). Simplification: sum of all outgoing edge weights from S to statuses that eventually reach a terminal.
   - Compute `backward_weight` = sum of edge weights FROM S to statuses that are farther from terminals.
   - In practice, use a simpler proxy: for each non-terminal status, compute the ratio of transitions that flow "downstream" (toward done) vs "upstream" (away from done). A status where most transitions go toward done is a main-path status. A status with balanced bidirectional flow (like "Blocked") gets sidelined.

   **Practical implementation:** Use a topological-sort-like approach on the frequency-weighted graph:
   a. Start from done statuses as sinks.
   b. For each pair of statuses (A, B) where edges exist in both directions, keep only the dominant direction (higher count). The reverse edge is the rework loop.
   c. After resolving cycles by dominant direction, the graph should be approximately a DAG.
   d. Topologically sort the DAG. Statuses that don't fit cleanly (very low total edge weight, or no path to a terminal) are sidelined.

4. **Order the result.** The topological sort produces the stage order. Done statuses are appended at the tail (per business rule 5).

5. **Sideline statuses.** Statuses excluded from the main path (low frequency, no clear path to done, predominantly bidirectional) go into the sidelined list. They are not discarded (business rule 6).

6. **Handle empty data.** If no transition edges exist, return empty stages, empty sidelined, zero confidence counts (business rule 7 + status code spec).

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/WorkflowDetectionService.cs` -- the service class + result record

**Result record shape:**
```
WorkflowDetectionResult
  List<string> Stages          (ordered main-path statuses)
  List<string> Sidelined       (excluded statuses)
  int TransitionCount
  int TicketCount
  int SprintCount
```

**DI registration:** The service takes no infrastructure dependencies -- it's a pure function over data. It receives transition edges and done statuses as method parameters, not constructor dependencies. No DI registration needed; the endpoint instantiates it or calls a static method.

Actually, to stay consistent with the `SprintIssueSyncService` pattern (which is constructor-injected), make it a class registered in DI. But since it has no dependencies, it can also be a static utility. **Decision: make it a regular class with a method, registered in DI via `AddScoped`. This follows the existing pattern and is easier to test later.**

**Dependencies:** None (this step defines the algorithm; Step 1 provides the data it consumes).

---

### Step 3: Create DetectWorkflowStages endpoint

**What:** Create the `GET /api/settings/workflow-stages/detect` endpoint. It is a read-only endpoint with no request body. It queries transition data via `TicketRepository`, reads done statuses from `AppSettingsRepository`, passes both to `WorkflowDetectionService`, and returns the detection result.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesEndpoint.cs` -- endpoint class
- `src/Services/Fokus/Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesQuery.cs` -- response record

**Endpoint details:**
- Extends `EndpointWithoutRequest<DetectWorkflowStagesResponse>` (no request body for GET)
- `[AllowAnonymous]` (no auth in v1)
- `[HttpGet("/api/settings/workflow-stages/detect")]`
- `[Tags("Settings")]`
- Constructor dependencies: `TicketRepository`, `AppSettingsRepository`, `WorkflowDetectionService`
- `HandleAsync`: call repository method from Step 1, read settings for done statuses, call detection service from Step 2, return result via `SendOkAsync`

**Response shape (in DetectWorkflowStagesQuery.cs):**
```
DetectWorkflowStagesResponse
  List<string> Stages
  List<string> Sidelined
  DetectionConfidence Confidence

DetectionConfidence
  int TransitionCount
  int TicketCount
  int SprintCount
```

**Skill:** Follow `create-feature` (FastEndpoints variant, `EndpointWithoutRequest`).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` -- same structure (GET, no request, AllowAnonymous, Settings tag).

**Dependencies:** Steps 1 and 2.

---

### Step 4: Add detection API function and types

**What:** Add the frontend API function to call the detection endpoint, and add the TypeScript types for the response.

**Files to modify:**
- `client/src/types/index.ts` -- add `DetectionResult` and `DetectionConfidence` interfaces
- `client/src/api/settings.ts` -- add `detectWorkflowStages()` function

**Type definitions:**
```typescript
interface DetectionConfidence {
  transitionCount: number
  ticketCount: number
  sprintCount: number
}

interface DetectionResult {
  stages: string[]
  sidelined: string[]
  confidence: DetectionConfidence
}
```

**API function:** `GET /settings/workflow-stages/detect`, returns `Promise<DetectionResult>`. Follow the existing pattern in `client/src/api/settings.ts`.

**Skill:** None (frontend patterns gap).

**Dependencies:** Step 3 (endpoint must exist for the API contract).

---

### Step 5: Extend settings store with detection state

**What:** Add detection-related state and actions to the existing `settingsStore`. This keeps all settings-related state in one store rather than creating a separate store.

**Files to modify:**
- `client/src/stores/settingsStore.ts` -- add detection state and actions

**New state:**
- `detectionResult: DetectionResult | null` -- the current detection proposal (null when no detection has run)
- `detecting: boolean` -- loading state for detection calls

**New actions:**
- `detectWorkflowStages()` -- calls the API, stores the result in `detectionResult`. On success, also populates `settings.workflowStages` with the detected stages (so the ordered list editor shows them as a proposal). Stores the sidelined list separately.
- `clearDetection()` -- resets `detectionResult` to null (called when the user saves or navigates away)
- `moveSidelinedToStages(status: string)` -- removes a status from the sidelined list and appends it to the workflow stages list in the form
- `removeSidelinedStatus(status: string)` -- removes from sidelined without adding to stages (if the user just wants to dismiss it)

**Behavior notes:**
- Detection does NOT auto-save. It populates the in-memory form state. The user must click Save (existing save flow) to persist.
- When detection runs, it replaces the current form's `workflowStages` with the detected stages. The previously saved stages are still in `settings` (the store's persisted state). If the user navigates away without saving, `syncFromStore()` in the Settings view restores the saved stages on next mount.

**Skill:** None (frontend patterns gap).

**Dependencies:** Step 4 (types and API function must exist).

---

### Step 6: Enhance Workflow Stages UI in SettingsView

**What:** Modify the existing Workflow Stages section in `SettingsView.vue` to support auto-detection, confidence display, sidelined statuses, and re-detect.

**Files to modify:**
- `client/src/views/SettingsView.vue` -- enhance the Workflow Stages `<section>`

**UI changes within the Workflow Stages section:**

1. **Auto-detection on mount.** In `onMounted`, after `fetchSettings` and `syncFromStore`: if `form.workflowStages` is empty, automatically call `store.detectWorkflowStages()`. On success, populate `form.workflowStages` with `store.detectionResult.stages`. This implements Flow 1 (auto-detection on first visit after sync).

2. **Confidence summary.** When `store.detectionResult` is not null, show a summary line above the ordered list: "Based on {transitionCount} transitions from {ticketCount} tickets across {sprintCount} sprints". Use muted text styling (`text-gray-400`). Hide when `detectionResult` is null (after save or manual entry).

3. **Re-detect button.** Add a "Re-detect" button next to the section header. Clicking it calls `store.detectWorkflowStages()` and replaces `form.workflowStages` with the fresh result. The button is disabled when `store.detecting` is true (loading state) or when confidence counts are all zero (no transition data -- implements Flow 3 rule). Style: secondary button (outline or muted, not primary blue).

4. **"Other statuses" area.** When `store.detectionResult` is not null and `store.detectionResult.sidelined` has items, show a sub-section below the ordered list editor titled "Other statuses". Display each sidelined status as a pill/chip with a "+" button that calls `moveSidelinedToStages(status)` to move it into the main ordered list. Once moved, it disappears from the sidelined area. Style: `bg-gray-800` pills, same as the done statuses pills pattern.

5. **Empty state message.** When `form.workflowStages` is empty and no detection result exists (no data), show: "Sync sprints to enable workflow detection" (Flow 3). The ordered list editor and manual add input remain visible below this message so manual entry still works.

6. **Cleanup on save.** After a successful save (in the `save()` function), call `store.clearDetection()` to hide the confidence summary and sidelined area. The section returns to its normal editing state (business rule 9 / Flow 1 step 9).

7. **Navigate-away behavior.** The existing pattern handles this: `syncFromStore()` runs on mount, restoring saved stages. The detection proposal only lives in the form's reactive state and `store.detectionResult`. On next mount, `syncFromStore` overwrites form state from the store's persisted settings. No additional code needed for discard-on-navigate (business rule 9 / Flow 2 step 6).

**Detecting state for Re-detect disable logic:** The Re-detect button needs to know whether transition data exists. Two approaches:
- (A) Run detection once to check, use the zero-confidence response to disable.
- (B) Track whether detection has ever returned non-zero confidence.

**Decision:** Use approach (A) -- the auto-detection call on empty stages already runs. If it returns zero confidence, the Re-detect button stays disabled. If the user has stages configured (no auto-detect), the Re-detect button is always enabled (the user explicitly asked to re-detect; if there's no data, they'll see empty results with zero confidence). This matches Flow 3 spec: "Re-detect button is disabled" only when no transition data exists, which maps to confidence counts being zero.

Simpler: disable Re-detect only when `store.detectionResult` is not null AND all confidence counts are zero. When `detectionResult` is null (fresh page load with existing stages), Re-detect is enabled -- clicking it will reveal whether data exists.

**Skill:** None (frontend patterns gap).

**Pattern reference:** The existing Workflow Stages section in `SettingsView.vue` (lines 135-160) -- extend it in place.

**Dependencies:** Steps 4 and 5.

## Cross-Service Changes

None. Single-service feature, read-only.

## Migration Notes

None. No database changes.

## Testing Strategy

**Backend:**
1. **Detection with real data.** Sync at least one sprint. Call `GET /api/settings/workflow-stages/detect`. Verify stages are ordered logically (e.g., "To Do" before "In Progress" before "Done"). Verify confidence counts are non-zero.
2. **Bug exclusion.** Verify that transitions from tickets with `IssueType = "Bug"` are not included in the detection input. Check that a bug-heavy workflow doesn't distort the detected pipeline.
3. **Empty data.** Call the endpoint before any sync. Verify empty stages, empty sidelined, zero confidence counts.
4. **Done statuses at tail.** Configure done statuses in settings. Run detection. Verify done statuses appear at the end of the stages list.
5. **Sidelined statuses.** Verify that low-frequency or bidirectional statuses (like "Blocked", "On Hold") appear in the sidelined list, not in the main stages.

**Frontend:**
6. **Auto-detection (Flow 1).** Clear workflow stages in settings. Navigate to Settings. Verify detection runs automatically and the proposal appears in the ordered list with confidence summary.
7. **Re-detect (Flow 2).** With stages configured, click Re-detect. Verify fresh proposal replaces the list. Navigate away without saving. Return to Settings. Verify previously saved stages are restored.
8. **No data (Flow 3).** Before syncing, visit Settings. Verify "Sync sprints to enable workflow detection" message. Verify Re-detect is disabled. Verify manual entry still works.
9. **Sidelined to main.** After detection, click "+" on a sidelined status. Verify it moves to the main ordered list. Save. Verify it persists.
10. **Save clears proposal state.** After detection, save settings. Verify confidence summary and sidelined area disappear.

## Open Questions

None.
