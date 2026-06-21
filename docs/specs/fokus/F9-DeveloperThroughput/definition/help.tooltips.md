# Developer Throughput — Tooltip Content

Sibling of `docs/features/DeveloperThroughput/spec.md`. Each section contains the **Short** variant (tooltip text for info icon hover, under 150 chars).

---

## SP Assigned

Total story points on non-removed tickets assigned to the developer in this sprint.

---

## SP Completed

Story points on tickets the developer finished — those whose final status is in the done statuses list.

---

## Completion %

Percentage of assigned story points the developer completed (SP Completed / SP Assigned).

---

## Tickets Done

Number of tickets the developer completed — those with a final status in the done statuses list.

---

## Tickets Carried Over

Non-removed tickets assigned to the developer that were not completed by sprint end.

---

## Delta Indicators

Change from the prior sprint — green for improvement, red for regression, gray for neutral.

---

## Rolling Average SP Completed

Smoothed delivery trend — average SP completed over 3 qualifying sprints (excludes 0% capacity sprints).

---

## Developer Sprint Capacity

Developer's availability for a sprint as a percentage (0-100%). Default is 100% (fully available).

---

## Throughput Trend Chart

Multi-line chart of SP completed per developer across sprints, using 3-sprint rolling averages.

---

## Sprint Selector

Choose a single sprint for detail view, or a range (Last 3/5/All) for trend analysis.

---

## Sub-Team Filter

Restrict all throughput content to developers in the selected sub-team.

---

## Assignee Attribution

Ticket credit goes to whoever is assigned at sync time — mid-sprint reassignments are not tracked.

---

## Zero-Ticket Developers

Developers with no tickets in a sprint appear with all-zero values — they are never hidden.

---

## Inactive Developers

Developers marked as inactive are excluded from all throughput views.
