# Jira Sync — Guide Page Help Content

Sibling of `docs/features/JiraSync/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Jira Connection

Fokus connects to Jira's REST API using three pieces of information: your Jira instance URL (e.g., yourcompany.atlassian.net), your Jira email address, and an API token. You can generate an API token from your Atlassian account settings. When you save credentials, Fokus immediately attempts to fetch your board list — if the dropdown populates with boards, your connection is working. If it fails, you'll see an inline error describing the problem (invalid credentials, unreachable instance, or rate limiting).

---

## Board Selection

After connecting to Jira, select the board whose sprints you want to track. Fokus supports a single board at a time — all sync operations pull data from this board. If your team uses multiple boards, choose the one that best represents your sprint work. You can change the board later in Settings, but switching boards does not remove previously synced data. The board selection is saved as part of your settings.

---

## Sprint Range Picker

The sprint range picker shows two dropdowns — From and To — populated with started sprints from your configured board (active and closed sprints only, ordered by start date). Future-state sprints that have never been started do not appear here. By default, the oldest sprint is selected as From and the newest as To. Narrow the range to sync specific sprints, or leave the defaults to backfill your full sprint history.

---

## Sync Sprints

Clicking "Sync Sprints" fetches data from Jira for every sprint in your selected range. For each sprint, Fokus creates or updates sprint records, ticket records, developer records, sprint memberships (which tickets were in which sprint), and status transition records. The sync processes sprints sequentially. A spinner is shown while the sync runs. When complete, a summary tells you how many sprints were synced, how many tickets were upserted, how many developers were discovered, and whether any sprints failed.

---

## Sync Backlog

"Sync Backlog" pulls two categories of data. First, it fetches sprints that have been created but never started — these are often used as planning buckets. Second, it finds tickets assigned to known epics that haven't appeared in any started sprint yet, ensuring your epic progress tracking has complete ticket coverage. Backlog data is excluded from trend analytics since these sprints haven't been executed. The summary shows how many backlog sprints were synced and how many additional epic tickets were discovered.

---

## Re-Sync (Data Correction)

Syncing is idempotent — you can safely re-sync any sprint at any time. When you re-sync, Fokus overwrites all data for that sprint (tickets, memberships, transitions) with the latest values from Jira. No duplicate records are created and no data from other sprints is affected. Use re-sync when you suspect stale data, when tickets were moved or re-estimated after your last sync, or when Jira statuses were corrected retroactively. Select the same sprint in both the From and To dropdowns and click "Sync Sprints."

---

## Sync Summary

After every sync operation, Fokus displays a summary with four key numbers: sprints attempted, sprints successfully synced, tickets upserted (created or updated), and developers discovered. If any sprints failed during the sync, the summary lists each failed sprint with the reason. Successfully synced data is always retained — a failure in one sprint does not roll back the others.

---

## Commitment Status

Fokus determines commitment status by analyzing the Jira changelog for each ticket. If a ticket was assigned to the sprint at or before the sprint start date, it's marked as "committed" — the team planned to do this work. If the ticket was added after the sprint started, it's marked as a scope change — work that wasn't part of the original plan. This distinction powers the scope change and disruption analytics downstream. The determination is fully automatic based on Jira's historical data; no manual tagging is needed.

---

## Removal Tracking

If a ticket is removed from a sprint after the sprint has started (detected via the Jira changelog's sprint field changes), Fokus records the removal timestamp on that ticket's sprint membership. Removed tickets contribute to the "removed SP" metric in disruption analysis, giving you visibility into work that was started but deprioritized or moved. This happens automatically during sync — no manual input is required.

---

## Final Status Capture

When a sprint is synced, Fokus captures each ticket's current Jira status and stores it on the sprint membership record. For closed sprints, this represents the ticket's final status — whether it completed, was still in progress, or was stuck in testing. This final status determines whether the ticket counts as "done" or "carried over" for analytics purposes. For active sprints, the status reflects the current state and will update on the next re-sync.

---

## Status Transitions

During sync, Fokus reads each ticket's full changelog from Jira and extracts every status change — the from-status, to-status, timestamp, and the person who made the change. These transition records are the foundation for cycle time analytics: how long tickets spend in each workflow stage, where bottlenecks occur, and how quickly work moves through your process. Transitions are extracted automatically for every ticket in every synced sprint.

---

## Story Points

Fokus reads story points from Jira's standard story point field — the one built into Jira, not a custom field. The value is captured at sync time and stored on the sprint membership, representing the ticket's estimate as of when you synced. If story points are changed in Jira after a sync, re-syncing the sprint will pick up the updated values. Tickets without story point estimates are still synced but won't appear in SP-based metrics.

---

## Active Sprint Handling

You can sync an active (in-progress) sprint to see current data, but Fokus excludes it from trend analytics like carry-over rate, throughput trends, and disruption trends. This prevents incomplete data from skewing your historical metrics. The active sprint's data is stored and visible in the system. When the sprint closes in Jira, re-sync it to capture the final state — the data updates in place and the sprint becomes eligible for analytics.

---

## Assignee Attribution

Fokus attributes a ticket to whoever is assigned to it in Jira at the moment you sync the sprint. If a ticket was reassigned during the sprint, only the final assignee is recorded. This means throughput and bug ratio metrics reflect the person who ended up owning the work, not everyone who touched it. For the most accurate attribution on closed sprints, sync soon after the sprint closes — before tickets are reassigned for the next sprint.

---

## Rate Limiting

To avoid overloading your Jira instance, Fokus caps its API requests at 10 per second. If Jira responds with a rate-limit error, Fokus uses exponential backoff — waiting progressively longer before retrying — rather than failing immediately. This means syncs may slow down under heavy load but will complete rather than abort. You don't need to take any action; the retry behavior is automatic.

---

## Partial Failure

When syncing a range of sprints, a failure in one sprint does not stop the others. Fokus continues syncing the remaining sprints and retains all successfully synced data. The sync summary at the end lists which sprints failed and the reason for each failure (e.g., Jira returned an error for a specific sprint). You can retry failed sprints individually by selecting them in the range picker and syncing again.
