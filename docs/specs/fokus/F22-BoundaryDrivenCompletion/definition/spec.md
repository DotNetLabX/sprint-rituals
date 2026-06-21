# Boundary-Driven Completion

**Traces to:** `docs/product/v1.md` §4.4 (Rev 1 — Done category definition), §5.1, §5.2, §5.3, §5.4, §5.5, §5.6, §5.7
**Source:** Scratch
**Dependencies:** F3 (Settings System), F6 (Workflow Auto-Detection), F12 (Cycle Time — defines boundary settings)
**Status:** Done
**Plan:** `docs/plans/BoundaryDrivenCompletion/plan.md`

---

## Purpose

All analytics features determine ticket completion by checking against a static "done statuses" list (currently "Done" and "Closed"). This produces incorrect numbers: tickets that reach "Testing" — which the team considers dev-complete — are counted as carry-over, deflating completion rates and inflating carry-over metrics. Meanwhile, the multi-sprint bar chart shows nearly 100% completion because it uses a different computation path, creating a visible contradiction with the single-sprint burnup.

The cycle time boundary settings already exist and let the user define where work starts and ends in the workflow. This feature makes the cycle time end boundary the single source of truth for completion across all analytics. Changing it redefines what "completed" means everywhere — measure dev throughput (end at Testing), QA throughput (end at Done), or any workflow boundary in between. The done statuses setting is retained but no longer drives completion.

## Entities

No new entities. This feature changes how existing settings are interpreted for completion.

**Settings reinterpretation:**

- **Cycle time end stage** (existing, F12) — currently used only for cycle time duration measurement. After this feature, it also defines the completion threshold for all analytics. A ticket is completed when its final status is at or after this stage in the ordered stage sequence.
- **Done statuses list** (existing, F3) — retained as a setting but no longer used for completion checks. It continues to provide the tail of the ordered stage sequence and remains visible and editable in the Settings UI.
- **Workflow stages** (existing, F6) — combined with done statuses to form the ordered stage sequence. No changes to how the sequence is constructed.

**Completion rule change:**

- **Before:** A ticket is completed when its final status matches any value in the done statuses list.
- **After:** A ticket is completed when its final status is at or after the cycle time end stage in the ordered stage sequence (workflow stages followed by done statuses).

Example with ordered stages [To Do, In Progress, Testing, Done, Closed]:

| Cycle time end stage | Completed statuses | What it measures |
|---|---|---|
| Testing | Testing, Done, Closed | Dev throughput |
| Done | Done, Closed | End-to-end completion |
| Closed | Closed | Fully resolved only |

## User Flows

```
Flow 1: Completion Reflects Cycle Time Boundaries
1. User has workflow stages configured as [To Do, In Progress, Testing, Done, Closed]
2. User has cycle time end stage set to "Testing"
3. User opens the Dashboard
4. SP Completed shows all feature tickets that reached Testing or beyond — not just Done/Closed
5. Completion % reflects the boundary-driven definition
6. User navigates to Sprints — bar chart and burnup both use the boundary-driven definition
7. User navigates to Developers — throughput uses the boundary-driven definition
8. All surfaces agree on what "completed" means
```

```
Flow 2: User Changes Cycle Time End Stage
1. User navigates to Settings > Cycle Time Boundaries
2. User changes cycle time end stage from "Testing" to "Done"
3. User saves
4. User returns to Dashboard
5. SP Completed now counts only tickets that reached Done or Closed
6. Completion % drops (fewer tickets qualify as completed)
7. Carry-over rate rises (more tickets counted as unfinished)
8. All analytics pages reflect the updated definition on next load
```

```
Flow 3: No Cycle Time End Stage Configured
1. User has workflow stages configured but has not set a cycle time end stage
2. The system defaults to the first done status (existing auto-detect behavior)
3. Completion behaves identically to before this feature — no regression
```

```
Flow 4: No Workflow Stages Configured
1. User has not synced yet or has not confirmed workflow stages
2. The ordered stage sequence contains only the done statuses
3. Completion falls back to matching against done statuses — identical to before this feature
4. Once the user configures workflow stages and a cycle time end stage, the boundary-driven definition takes effect
```

## API Surface

No new endpoints. No changes to request parameters or response shapes. This feature changes only the internal computation of "completed" fields across all analytics endpoints.

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/sprint-summary | Authenticated | Completion %, SP Completed use boundary-driven definition | 200, 400 |
| GET | /api/analytics/scope-change | Authenticated | completedSp in bar chart and burnup uses boundary-driven definition | 200, 400 |
| GET | /api/analytics/developer-throughput | Authenticated | SP Completed, Completion %, Tickets Done, Tickets Carried Over use boundary-driven definition | 200, 400 |
| GET | /api/analytics/carry-over | Authenticated | Carry-over = inverse of boundary-driven completion | 200, 400 |
| GET | /api/analytics/bug-ratio | Authenticated | "Completed" bugs and stories uses boundary-driven definition | 200, 400 |
| GET | /api/analytics/epic-progress | Authenticated | Done tickets and SP use boundary-driven definition | 200, 400 |
| GET | /api/analytics/cycle-time | Authenticated | "Completed ticket" pre-filter uses boundary-driven definition | 200, 400 |

## Business Rules

1. **Completion is position-based.** A ticket is completed when its final status appears at or after the cycle time end stage in the ordered stage sequence. "At or after" means the status is at the same position or a later position in the sequence.

2. **Ordered stage sequence construction is unchanged.** The sequence is workflow stages followed by done statuses — same as today. This feature changes what the sequence is used for, not how it is built.

3. **Done statuses list is retained but not used for completion.** The setting remains visible and editable in the Settings UI. It continues to contribute the tail of the ordered stage sequence. It is no longer checked directly when determining whether a ticket is completed.

4. **Auto-detect fallback is unchanged.** When no cycle time end stage is explicitly configured, the system defaults to the first done status. This preserves current behavior for users who have not configured cycle time boundaries.

5. **Empty workflow stages fallback.** When workflow stages are not configured, the ordered stage sequence contains only the done statuses. Completion falls back to matching against those statuses — identical to behavior before this feature. If the configured cycle time end stage does not appear in the ordered stage sequence (e.g., workflow stages were cleared after configuration), the system falls back to the first done status.

6. **Feature-only filtering layers on top.** The feature-only metrics split (F21) applies after the completion check. Order: excluded-from-scope filtering first, then boundary-driven completion check, then bug/feature split. This feature changes step 2 only.

7. **Multi-sprint bar chart stays total-scope.** The bar chart completed SP includes all ticket types (features and bugs) using the boundary-driven definition. The single-sprint burnup remains feature-only per F21. This preserves the deliberate scope split established by F21.

8. **Health score uses boundary-driven completion.** The completion sub-score input uses the boundary-driven completion %. Thresholds, weights, and the composite formula are unchanged.

9. **Carry-over is the inverse.** A ticket is carried over when it is not completed by the boundary-driven definition and is not removed. No separate carry-over definition needed.

10. **All deltas and sparklines use boundary-driven values.** Every delta comparison and sparkline data point recalculates using the boundary-driven definition. Historical sprints recompute on query — completion is not snapshotted.

11. **Unrecognized statuses are not completed.** If a ticket's final status does not appear anywhere in the ordered stage sequence, it is not considered completed. This matches existing behavior for unrecognized statuses.

12. **Epic Progress uses current status, not sprint-end status.** Most analytics features check the ticket's status at sprint close (stored in sprint membership). Epic Progress instead checks the ticket's live current status for progress tracking, and the sprint-end status for velocity tracking. The boundary-driven completion check applies to both paths identically: the status (whichever one applies) must appear at or after the cycle time end stage in the ordered sequence. Epic Progress's exclusion from excluded-from-scope filtering (F14) is unchanged.

13. **Cycle time pre-filter and measurement boundary are the same setting.** The cycle time end stage serves two purposes after this feature: (a) the completion pre-filter that determines which tickets are included in cycle time calculations, and (b) the measurement endpoint that defines where the cycle time duration stops. Both use the same configured value — a ticket that reaches the end stage is both "cycle complete" (for duration measurement) and "completed" (for all analytics). This supersedes any prior definition that references the done statuses list for the cycle time pre-filter.

## Acceptance Criteria

### Completion Definition

- [ ] With cycle time end stage = "Testing" and ordered stages [To Do, In Progress, Testing, Done, Closed], tickets with final status Testing, Done, or Closed are counted as completed
- [ ] With cycle time end stage = "Done", tickets with final status Done or Closed are counted as completed; tickets in Testing are carry-over
- [ ] With cycle time end stage not configured, completion defaults to the first done status — identical to behavior before this feature
- [ ] With workflow stages not configured, completion falls back to matching done statuses only — identical to behavior before this feature
- [ ] Tickets with final status not in the ordered stage sequence are not counted as completed

### Dashboard (F8)

- [ ] SP Completed reflects boundary-driven completion (feature-only per F21)
- [ ] Completion % reflects boundary-driven completion (feature-only per F21)
- [ ] Health score completion sub-score uses boundary-driven completion %
- [ ] Deltas and sparklines use boundary-driven values
- [ ] Developer leaderboard SP completed uses boundary-driven definition
- [ ] Zero-SP developers flag uses boundary-driven completion to determine who completed 0 SP
- [ ] Bug SP annotation, disruption rates, and carry-over rate cards unaffected in structure

### Sprints Page (F10)

- [ ] Multi-sprint bar chart completed SP uses boundary-driven definition (total-scope — all ticket types)
- [ ] Single-sprint burnup completed SP uses boundary-driven definition (feature-only per F21)
- [ ] Committed SP, added SP, removed SP, disruption rate, and net scope change are unchanged

### Developer Throughput (F9)

- [ ] SP Completed uses boundary-driven definition (feature-only per F21)
- [ ] Completion % uses boundary-driven definition
- [ ] Tickets Done and Tickets Carried Over use boundary-driven definition
- [ ] Rolling averages use boundary-driven values
- [ ] Deltas compare boundary-driven values to the prior sprint

### Carry-Over (F11)

- [ ] Carry-over SP = tickets not completed by boundary-driven definition
- [ ] Carry-over rate uses boundary-driven completion as the complement
- [ ] Status distribution of carry-over reflects the updated completion boundary

### Bug Ratio (F13)

- [ ] "Completed" bugs and stories per developer uses boundary-driven definition

### Epic Progress (F14)

- [ ] Done tickets and SP done use boundary-driven definition
- [ ] Completion percentage uses boundary-driven definition
- [ ] Projected sprints to completion uses boundary-driven velocity

### Cycle Time (F12)

- [ ] "Completed ticket" pre-filter uses boundary-driven definition instead of done statuses list

### Settings

- [ ] Done statuses setting remains visible and editable — no UI changes
- [ ] Changing cycle time end stage changes completion across all analytics on next page load
- [ ] No new settings UI required

## Out of Scope

- **Bar chart vs burnup numerical discrepancy** — the multi-sprint bar chart and single-sprint burnup show different completed SP values for the same sprint even under the same definition. This appears to be a separate snapshotting or computation-path issue. Tracked independently as a bug.
- **Removing the done statuses setting** — retained for ordered-stage-sequence contribution and backward compatibility. May be reconsidered in a future cleanup.
- **New Settings UI** — cycle time boundary dropdowns already exist. No new configuration surfaces needed.
- **Renaming "Done Statuses" label in Settings** — the label could be misleading now that it does not drive completion, but relabeling is cosmetic and deferred.
- **Updating other feature specs** — this spec covers the cross-cutting completion change. Individual feature specs (F8–F14, F21) are not revised; they reference "Done category" which is now defined in v1.md §4.4 Rev 1.
