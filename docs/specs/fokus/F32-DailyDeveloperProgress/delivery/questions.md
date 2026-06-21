# Daily Developer Progress — Questions

## Q1: SyncBacklogSprints broadcast variable name
**From:** developer
**To:** architect
**Status:** Answered
**Step:** 3 — Create SignalR hub and wire sync broadcast
**File:** `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs`

**Context:** The plan instructs broadcasting `SprintIds = sprintsInRange.Select(s => s.Id).ToArray()` in both sync endpoints after successful sync. In `SyncSprintsEndpoint`, the variable `sprintsInRange` exists and is exactly the right list. In `SyncBacklogSprintsEndpoint`, there is no `sprintsInRange` — the sprint variable is `futureSprints` (future/backlog sprints fetched from Jira). These are always `SprintState.Future`, so they can never be the active sprint that the Daily Progress tab watches.

**Question:** Should the backlog sync endpoint broadcast use `futureSprints` as the sprint ID source (naming fix only), or should the backlog endpoint be excluded from the broadcast entirely since it only touches future sprints that cannot trigger a Daily Progress refresh? If excluded, Step 3 only modifies `SyncSprintsEndpoint`.

### Answer
Option B — skip `SyncBacklogSprintsEndpoint` entirely. Backlog sync only touches `SprintState.Future` sprints, which can never be the active sprint. Broadcasting from it would be semantically misleading and a no-op for Daily Progress clients. Plan updated: Step 3 now only modifies `SyncSprintsEndpoint`.
