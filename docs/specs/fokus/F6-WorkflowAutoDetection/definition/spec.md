# Workflow Auto-Detection

**Traces to:** `docs/product/v1.md` §5.4 (Cycle Time — configurable workflow status mapping)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync)
**Status:** Done
**Plan:** `docs/plans/WorkflowAutoDetection/plan.md`

---

## Purpose

F12 (Cycle Time) needs an ordered list of workflow stages to measure how long tickets spend in each phase. Manually configuring these stages requires the user to know their Jira workflow by heart. This feature eliminates that friction: it analyzes the status transition history already collected during sync and proposes an ordered workflow pipeline. The user reviews and confirms — no guessing, no Jira admin access needed.

## Entities

This feature introduces no new domain entities. It reads existing status transition data and writes to the existing workflow stages setting.

**Existing data queried:**
- Status transitions (from-status, to-status, timestamp, ticket association)
- Tickets (issue type — used to filter detection input)
- Sprints (used for confidence reporting)
- App settings (workflow stages list, done statuses list)

## User Flows

```
Flow 1: Auto-Detection on First Visit After Sync
1. User has synced at least one sprint (status transition data exists)
2. User navigates to Settings
3. The Workflow Stages section detects that no stages are configured
4. Detection runs automatically: the system analyzes status transition history and proposes an ordered stage list
5. The proposed stages appear in the ordered list editor, with a confidence summary
   ("Based on N transitions from M tickets across K sprints")
6. Statuses found in the data but excluded from the main path appear in a separate
   "Other statuses" area
7. User reviews the proposal: reorders stages, removes irrelevant ones, or adds
   statuses from the "Other statuses" area
8. User clicks Save — stages are persisted to app settings
9. The confidence summary and "Other statuses" area disappear; the section returns
   to its normal editing state
```

```
Flow 2: Manual Re-Detection
1. User navigates to Settings > Workflow Stages (stages may or may not already be configured)
2. User clicks the "Re-detect" button
3. Detection runs and replaces the current list with a fresh proposal
4. The confidence summary and "Other statuses" area appear
5. User reviews and saves (same as Flow 1, steps 7-9)
6. If the user does not save, navigating away discards the proposal and restores
   the previously saved stages
```

```
Flow 3: No Transition Data Available
1. User navigates to Settings > Workflow Stages before syncing any sprints
2. No status transition data exists in the system
3. The section shows the empty ordered list with a message:
   "Sync sprints to enable workflow detection"
4. The Re-detect button is disabled
5. Manual entry still works — user can type stages by hand if they know their workflow
```


## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/settings/workflow-stages/detect | None | -- | Detection result | 200 |

**Detection result response shape:**

Top level:
- `stages` — ordered list of status names representing the detected forward workflow path (e.g., ["To Do", "In Progress", "Code Review", "Testing", "Done"]). Empty when no transition data exists.
- `sidelined` — list of status names found in transition data but excluded from the main path (low-frequency or bidirectional). Empty when no transition data exists.
- `confidence` — object with: transition count (total transitions analyzed), ticket count (distinct non-bug tickets with transitions), sprint count (distinct sprints covering those transitions)

**Status codes:**
- 200: Detection result returned. When no transition data exists, `stages` and `sidelined` are empty and confidence counts are zero — the frontend uses this to show the "sync first" message and disable Re-detect.

**Saving detected stages:** Uses the existing `PUT /api/settings` endpoint. The frontend merges the confirmed stage list into the full settings payload (alongside board ID, done statuses, health thresholds, and health weights) before saving — the endpoint performs a full replacement. No new save endpoint needed.

## Business Rules

1. **Detection input excludes bugs.** The transition graph is built from tickets whose issue type is not "Bug". Bugs follow chaotic, non-linear status paths that distort the detected pipeline. Stories, tasks, epics, and sub-tasks are included.

2. **The transition graph is frequency-weighted.** Each edge (from-status to to-status) is weighted by the number of times that transition occurred across all included tickets. High-frequency edges represent the team's actual workflow; low-frequency edges are exceptions.

3. **Forward-flow scoring identifies the main path.** The configured done statuses serve as anchor points — they are the known terminal statuses. Each remaining status is scored by the ratio of its transitions toward these terminals versus its transitions away from them. Statuses with high forward-flow scores form the main pipeline. Statuses with balanced or low scores (like "Blocked" or "On Hold") are sidelined. This approach handles cycles in the graph without requiring a pure topological sort, which would fail on real workflows where tickets move backward (e.g., "Code Review" back to "In Progress").

4. **Cycles are resolved by dominant direction.** Real workflows have backward transitions (e.g., "Code Review" back to "In Progress" for rework). When the graph contains cycles, the dominant direction (the direction with more transitions) determines ordering. The less-frequent reverse direction is treated as a rework loop, not a pipeline step.

5. **Done statuses appear at the tail.** The detected pipeline includes statuses that match the configured done statuses list, positioned at the end of the stage order. This gives F12 a complete pipeline from first status to terminal status.

6. **Sidelined statuses are not discarded.** Statuses excluded from the main path are returned separately so the user can manually add them if they're meaningful to their workflow.

7. **Detection always produces a result.** If any status transition data exists, detection returns a proposal — even from sparse data. The confidence metadata lets the user judge reliability. There is no minimum data threshold that blocks detection.

8. **Detection is read-only.** The detect endpoint analyzes transition data and returns a proposal. The proposal becomes the configured workflow stages only when the user saves through the existing settings endpoint.

9. **Re-detection replaces the in-memory proposal, not the saved stages.** Clicking "Re-detect" updates the UI with a fresh proposal. If the user navigates away without saving, the previously saved stages remain unchanged.

10. **One pipeline for all issue types.** The detected workflow is a single ordered list. F12 measures all ticket types against the same pipeline (with per-type breakdowns in its own display). Per-issue-type pipelines are not supported.

11. **Auto-detection triggers once per visit.** When workflow stages are empty and the user visits Settings, detection runs automatically. Once stages are saved, auto-detection does not re-trigger — only the manual "Re-detect" button runs it again.

12. **Settings save is full-replacement.** The existing settings endpoint replaces all fields. When saving detected stages, the frontend must merge the confirmed stage list into the complete settings payload (alongside board ID, done statuses, health thresholds, and health weights). The current Settings UI already handles this — detection does not change the save mechanism.

## Acceptance Criteria

- [ ] GET /api/settings/workflow-stages/detect returns an ordered list of status names derived from transition data
- [ ] Detection excludes transitions from tickets with issue type "Bug"
- [ ] Detection includes transitions from stories, tasks, epics, and sub-tasks
- [ ] Returned stages are ordered by the dominant forward-flow direction in the transition graph
- [ ] Statuses with predominantly bidirectional or low-frequency transitions appear in the sidelined list, not the main stages
- [ ] Done statuses from app settings appear at the tail of the detected stage list
- [ ] Response includes confidence metadata: transition count, ticket count, sprint count
- [ ] Response returns empty stages with zero confidence counts when no transition data exists
- [ ] Visiting Settings with empty workflow stages auto-triggers detection and displays the proposal
- [ ] The proposal appears in the existing ordered list editor (reorder, add, remove controls)
- [ ] Sidelined statuses appear in a separate "Other statuses" area below the main list
- [ ] User can move statuses from the sidelined area into the main ordered list
- [ ] Confidence summary is visible when a detection result is displayed
- [ ] Clicking "Re-detect" runs a fresh detection and replaces the current proposal
- [ ] Re-detect button is disabled when no transition data exists
- [ ] Saving settings with detected stages persists them via the existing PUT /api/settings endpoint
- [ ] Navigating away without saving discards the proposal and restores previously saved stages
- [ ] Manual stage entry works independently of detection (user can type stages without syncing)

## Out of Scope

- **Per-issue-type workflow pipelines** — one shared pipeline for v1. Different issue types may follow different paths, but cycle time (F12) measures all types against the same stage sequence.
- **Auto-grouping of similar status names** — statuses like "Code Review" and "Peer Review" are not automatically merged. Users with redundant Jira statuses remove duplicates manually in the review step.
- **Jira workflow definition API import** — detection uses observed transition history, not Jira's configured workflow graph. This is more accurate (reflects actual team behavior) and works without Jira admin permissions.
- **Webhook-triggered or scheduled detection** — detection runs on Settings page visit (auto when empty) or button click (manual). No background detection process.
- **Status category import from Jira** — Jira's native To Do / In Progress / Done categories are not used as input. Detection relies entirely on transition frequency analysis.
- **Confidence-based blocking** — detection always produces a result regardless of data volume. No minimum threshold blocks the proposal.
- **Cycle time boundary selection** — F6 delivers an ordered pipeline of stages. F12 (Cycle Time) will let users pick which stage starts and ends the cycle time measurement from that ordered list. Boundary selection is F12's concern, not F6's.
- **Post-sync notification** — a toast or banner after sync directing users to Settings when stages are empty. This requires notification infrastructure that does not exist in the app. The same user journey is covered by Flow 1 (auto-detection triggers when the user visits Settings with empty stages).
