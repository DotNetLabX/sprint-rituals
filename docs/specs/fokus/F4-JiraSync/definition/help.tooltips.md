# Jira Sync — Tooltip Help Content

Sibling of `docs/features/JiraSync/spec.md`. Each section provides the Short variant (tooltip text, under 150 chars, shown on info icon hover).

---

## Jira Connection

Link Fokus to your Jira instance using your email and an API token.

---

## Board Selection

Choose which Jira board to sync sprints from. Only one board is supported.

---

## Sprint Range Picker

Select a start and end sprint to define which sprints to sync from Jira.

---

## Sync Sprints

Pull sprint, ticket, developer, and transition data from Jira for the selected range.

---

## Sync Backlog

Fetch future-state sprints and unstarted epic tickets that aren't in any active sprint.

---

## Re-Sync (Data Correction)

Re-syncing a sprint overwrites its data with fresh values from Jira. No duplicates created.

---

## Sync Summary

Post-sync report showing sprints synced, tickets upserted, developers found, and any failures.

---

## Commitment Status

Whether a ticket was in the sprint at start (committed) or added later (scope change).

---

## Removal Tracking

Detects when a ticket was pulled from a sprint mid-sprint and records the removal timestamp.

---

## Final Status Capture

Records each ticket's Jira status at sync time to determine completion or carry-over.

---

## Status Transitions

Every status change in Jira becomes a transition record powering cycle time analytics.

---

## Story Points

Story points are read from Jira's built-in story point field at sync time.

---

## Active Sprint Handling

Active sprints are synced and stored but excluded from trend analytics until they close.

---

## Assignee Attribution

The person holding the ticket at sync time gets attribution. Mid-sprint reassignments aren't tracked.

---

## Rate Limiting

Fokus limits Jira API calls to 10/second and retries automatically on rate-limit responses.

---

## Partial Failure

If one sprint fails to sync, the rest still complete. The summary reports what failed and why.
