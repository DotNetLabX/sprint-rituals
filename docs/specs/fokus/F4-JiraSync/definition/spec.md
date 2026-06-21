# Jira Sync

**Traces to:** `docs/product/v1.md` §3 (Data Source), §3.3 (Sync Model)
**Covers:** F4 (Jira Integration), F5 (Sprint Sync)
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System)
**Status:** Done
**Plan:** `docs/plans/JiraSync/plan.md`

---

## Purpose

Fokus needs sprint and backlog data from Jira before any analytics can run. This feature establishes the connection to Jira's REST API and provides the sync actions that populate the local database with sprint, ticket, developer, and transition data. It is the single data ingress path — every analytics feature depends on data this feature produces.

## Entities

This feature introduces no new domain entities. It populates existing entities defined by F2: Sprint, Developer, Ticket, SprintMembership, StatusTransition.

It extends the data flow by:
- Mapping Jira API responses into these entities
- Deriving membership commitment status from changelog analysis (was the ticket added at or before sprint start?)
- Deriving membership removal timestamp from changelog (was the ticket removed from the sprint mid-sprint?)
- Capturing final status on each membership (the ticket's status when the sprint was closed/synced)
- Extracting status transitions from Jira's issue changelog (each status change becomes a transition record)
- Snapshotting story points at sync time on each sprint membership

## User Flows

```
Flow 1: Connect to Jira
1. User navigates to Settings
2. User enters Jira instance URL, email, and API token
3. User saves credentials
4. System attempts to fetch the board list from Jira using the provided credentials
5. If successful: board dropdown populates with available boards — connection confirmed
6. If failed: inline error shown (invalid credentials, unreachable instance, rate limited)
7. User selects a board from the dropdown
8. User saves the board selection
```

```
Flow 2: Initial Backfill (Sync Sprint Range)
1. User navigates to the Sync screen
2. Sprint range picker shown: two dropdowns (From / To), populated from Jira
3. Dropdowns contain started sprints only (active + closed), ordered by start date
4. Auto-selected: oldest sprint (From), newest sprint (To)
5. User adjusts range if desired
6. User clicks "Sync Sprints"
7. System syncs each sprint in range sequentially
8. UI shows a spinner until complete (no per-sprint progress detail)
9. On completion: sync summary displayed
10. If any sprints failed: summary lists which ones; successfully synced data is retained
```

```
Flow 3: Ongoing Sprint Sync
1. A sprint closes in Jira
2. User opens Fokus, navigates to Sync
3. User selects the just-closed sprint in both From and To dropdowns
4. User clicks "Sync Sprints"
5. System syncs that single sprint (idempotent — safe to re-sync)
6. Summary shown on completion
```

```
Flow 4: Sync Backlog
1. User clicks "Sync Backlog"
2. System fetches all future-state sprints from the configured board and their issues
3. System fetches tickets assigned to known epics that are not in any started sprint
4. Data persisted (idempotent)
5. Summary shown: backlog sprints synced, additional epic tickets discovered
```

```
Flow 5: Re-sync (Data Correction)
1. User suspects stale data for a specific sprint
2. User selects that sprint in both From and To
3. Clicks "Sync Sprints"
4. System overwrites all data for that sprint with fresh Jira data — no duplicates
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/boards | None | — | List of boards (id, name, type) | 200, 401, 502 |
| GET | /api/jira/sprints | None | ?boardId={id} | List of started sprints (id, name, startDate, state) ordered by startDate | 200, 400, 502 |
| POST | /api/sync/sprints | None | { fromSprintId, toSprintId } | Sync summary | 200, 400, 502 |
| POST | /api/sync/backlog | None | — | Backlog sync summary | 200, 502 |

**Sync summary shape:** sprints attempted, sprints synced, tickets upserted, developers discovered, failures (list of sprint IDs + error reason).

**Backlog sync summary shape:** backlog sprints synced, epic tickets discovered.

**Error conditions:**
- 401: Jira rejected the stored credentials (expired or invalid token)
- 400: Missing required parameter (boardId) or invalid sprint range (from after to)
- 502: Jira API unreachable or returned an unexpected error; body includes Jira's error message when available

## Business Rules

1. **Idempotent sync.** Re-syncing the same sprint overwrites all associated data with fresh values. No duplicates created, no historical data lost for other sprints.

2. **Commitment derivation.** A ticket is "committed" if it was in the sprint at or before the sprint start date. Tickets added after start date are scope changes. This is derived from Jira's changelog (the sprint field change timestamp).

3. **Sprint range ordering.** The range picker shows only started sprints (active or closed), ordered by start date. Future-state sprints are excluded from this list.

4. **Active sprint exclusion from analytics.** Active sprints are synced (data stored) but excluded from trend analytics. They appear in the system for freshness — when the sprint closes and is re-synced, the data updates in place.

5. **Future-state sprints as backlog.** Sprints created but never started (used as categorization buckets) are synced via "Sync Backlog" and excluded from analytics.

6. **Rate limiting.** Maximum 10 requests per second to Jira. On rate-limit responses, exponential backoff before retry — not immediate failure.

7. **Partial failure tolerance.** If one sprint in a range fails, the remaining sprints still sync. The summary reports which sprints failed and why. Successfully synced data is retained.

8. **Epic data on tickets.** Epic association lives on tickets (epic key + epic name). Backlog sync ensures tickets not yet in a started sprint are captured for epic progress completeness.

9. **Implicit connection validation.** Saving Jira credentials triggers a board list fetch. If it succeeds, the board dropdown populates (proof of valid connection). If it fails, an inline error surfaces. No separate "test connection" button.

10. **Standard story point field.** Story points are read from Jira's built-in story points field, not a custom field.

11. **Assignee at sync time.** The person who holds the ticket when the sprint is synced receives attribution. Mid-sprint reassignment history is not tracked for attribution purposes.

12. **Status transition extraction.** Every status change in a ticket's Jira changelog becomes a transition record: from-status, to-status, timestamp, author. These power cycle time analytics downstream.

13. **Removal tracking.** If a ticket is removed from a sprint mid-sprint (detected via changelog: sprint field change that removes the ticket), the removal timestamp is recorded. This powers disruption analysis (removed SP metric).

14. **Final status capture.** Each ticket's status at the time a sprint is synced is recorded on the membership. For closed sprints, this is the ticket's final status in that sprint — it determines whether the ticket completed or carried over.

## Acceptance Criteria

- [ ] Saving valid Jira credentials in Settings populates the board dropdown with boards from Jira
- [ ] Saving invalid credentials shows an inline error without crashing or silently failing
- [ ] Selecting and saving a board persists the selection in settings
- [ ] GET /api/jira/sprints returns started sprints for the configured board, ordered by start date
- [ ] POST /api/sync/sprints with a valid range creates Sprint, Ticket, Developer, SprintMembership, and StatusTransition records
- [ ] Re-syncing an already-synced sprint overwrites data without creating duplicates
- [ ] Tickets added after sprint start have commitment status = false
- [ ] Tickets present at or before sprint start have commitment status = true
- [ ] Active sprints can be synced successfully (stored with Active state)
- [ ] POST /api/sync/backlog fetches future-state sprints and their issues
- [ ] POST /api/sync/backlog fetches tickets belonging to known epics not in any started sprint
- [ ] If one sprint in a range fails, remaining sprints sync and the summary reports the failure
- [ ] Rate-limit responses from Jira trigger backoff and retry, not immediate failure
- [ ] Sync summary includes: sprints attempted, sprints synced, tickets upserted, failures list
- [ ] Story points are read from the standard Jira field
- [ ] Status transitions are extracted from issue changelogs and persisted as transition records
- [ ] Tickets removed from a sprint mid-sprint have their removal timestamp recorded
- [ ] Each membership records the ticket's status at the time of sync (final status for closed sprints)

## Out of Scope

- **Scheduled/automatic sync** — manual trigger only in v1. No cron, no webhook listener.
- **Multi-board support** — single board configured in Settings. Multi-board deferred.
- **Progress streaming during sync** — simple spinner, no per-sprint progress updates. No real-time push in v1.
- **Custom story point fields** — standard field only. Custom field mapping deferred.
- **Standalone Epic entity** — epic data lives on tickets. Richer epic metadata (target dates, epic status, owner) deferred.
- **Sync history/audit log** — no record of past syncs. Only the "last synced" timestamp on each sprint.
- **Webhook-based sync** — no Jira webhook listener. User triggers sync manually.
